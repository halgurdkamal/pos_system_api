# 99. Known Gaps

A single canonical list of issues in the API today. Each item names the affected endpoint(s), the impact, and the workaround your client must apply until the underlying handler is fixed.

The other guides reference this page instead of duplicating the warnings inline.

## Contents

- [Critical security gaps](#critical-security-gaps)
- [Functional TODOs (handlers that don't fully do what their name suggests)](#functional-todos)
- [Behavioural quirks (working as built, but surprising)](#behavioural-quirks)
- [Missing endpoints / features](#missing-endpoints--features)

---

## Critical security gaps

### G-1. `InventoryController` lacks class-level `[Authorize]`

**Affected**: every endpoint under `/api/inventory/*` **except** `/pos-items` and `/pos-items/by-barcode/{barcode}` (which carry their own `ShopAccess` policy).

**Impact**: `AddStock`, `ReduceStock`, pricing changes, packaging overrides, and stock-zone moves are **reachable without a token**. An anonymous attacker on the network could deplete inventory, change prices, or zero-out shop stock.

**Fix**: add `[Authorize(Policy = "ShopAccess")]` at the controller class level in `src/API/Controllers/InventoryController.cs`. This is the single highest-priority fix in the project.

### G-2. `Shop.UpdateReceiptConfig` and `UpdateHardwareConfig` lack the `ShopOwnerOrAdmin` policy

**Affected**: `PUT /api/shops/{id}/receipt-config`, `PUT /api/shops/{id}/hardware-config`.

**Impact**: any authenticated user can rewrite another shop's receipt branding (logo URL, footer text, VAT line) or hardware configuration (printer IPs, terminal IDs).

**Fix**: add `[Authorize(Policy = "ShopOwnerOrAdmin")]` to both action methods in `ShopsController.cs`.

### G-3. `POST /api/auth/register` is public and doesn't gate role escalation

**Affected**: `POST /api/auth/register`.

**Impact**: anyone with network access can call `register` with `"role": "SuperAdmin"`. The handler parses the enum but does **not** require an existing SuperAdmin token to mint another.

**Fix options**:
- Put the route behind an environment-flag in production (recommended).
- Add a check: if `role == SuperAdmin`, require the caller to already be SuperAdmin.
- Bootstrap the very first admin by inserting a row directly in the DB.

### G-4. `POST /api/barcodes/generate/barcode` and `/qrcode` are public

**Affected**: both barcode/QR-code generation endpoints under `/api/barcodes/generate/*`.

**Impact**: CPU-bound image-generation endpoints accept arbitrary input from anonymous clients — easy denial-of-service target.

**Fix**: add `[Authorize]` (any auth is enough) and front-end rate-limit per user.

### G-5. `POST /api/pdf/receipt/custom` is `[AllowAnonymous]`

**Affected**: `POST /api/pdf/receipt/custom`.

**Impact**: marked anonymous "for testing purposes". An attacker can render arbitrary text claiming to be a real shop, complete with their chosen `logoUrl` (also an SSRF primitive — the server fetches the URL).

**Fix**: gate behind `[Authorize]` for production; validate `logoUrl` host against an allow-list.

---

## Functional TODOs

### F-1. PO `/receive` crashes when adding the first batch for a drug

**Affected**: `POST /api/purchaseorders/{id}/receive`.

**Status**: the original "doesn't add stock at all" gap was addressed in commit `c2d2315` — `ReceiveStockCommandHandler.UpdateInventoryAsync` now constructs a `Batch` and lazily creates a `ShopInventory` row. **But the new code is broken on the first-receipt path.**

**Where it breaks**: when no `ShopInventory` exists yet for `(shopId, drugId)`, the handler calls `_inventoryRepository.AddAsync(inventory, …)` (marks the entity `Added`), then immediately calls `_inventoryRepository.UpdateAsync(inventory, …)`, whose body is `_context.ShopInventory.Update(inventory)` (re-marks the entity `Modified`). On `SaveChangesAsync`, EF issues an UPDATE for a row that was never INSERTed and the call fails with:

```
The database operation was expected to affect 1 row(s), but actually affected 0 row(s)
```

If a `ShopInventory` already exists, the call succeeds and the batch is appended.

**Impact**: receiving a PO for a drug that has never been stocked in the destination shop returns `500` and rolls back the receipt entirely — the PO line stays at `receivedQuantity = 0`. Subsequent receipts for the same drug into the same shop work.

**Workaround**: prime the `(shopId, drugId)` row first via `POST /api/inventory/shops/{shopId}/stock` with `quantity = 0` (or your real opening quantity), then call `/receive`. Or skip `/receive` altogether for inventory bookkeeping and use `POST /api/inventory/shops/{shopId}/stock` directly — the PO line just won't show the receipt history. See [Recipe 3 in the cookbook](./08-data-model-and-recipes.md#recipe-3--receive-stock-from-a-purchase-order-current-reality).

**Fix when ready**: drop the redundant `UpdateAsync` call when the inventory was just `AddAsync`-ed, or change `InventoryRepository.UpdateAsync` to no-op when the entity is already in `Added` state.

### F-2. Sales `/complete` does not deduct stock — *deduction now happens on `/payment` instead*

**Affected**: `POST /api/salesorders/{id}/payment` (deducts), `POST /api/salesorders/{id}/complete` (does not).

**Status**: deduction is now wired up — but on payment, not on completion. `ProcessPaymentCommandHandler` calls `ISalesStockService.DeductForSaleAsync(...)` immediately after flipping status `Confirmed → Paid` (commit `56d03c0`). `CompleteSalesOrderCommandHandler` is unchanged: it only flips status to `Completed`.

**Impact / contract for clients**: stock is debited the moment a sale is paid. Cancelling a `Paid` order or refunding a `Paid`/`Completed` order restores stock automatically (see F-3). The till **does not need** the historical workaround of calling `PUT /api/inventory/.../reduce` after `/complete`.

**Edge**: if `/payment` succeeds in flipping the order's status but the subsequent inventory call fails (e.g. transient DB error), the order is `Paid` while inventory still reads the old level. The handler comment flags this — the call is not yet wrapped in a single DB transaction. Until that's fixed, surface payment failures explicitly in the till and add a reconciliation script.

### F-3. Refunds restore stock — *now wired*

**Affected**: `POST /api/salesorders/{id}/refund`, `POST /api/salesorders/{id}/cancel` (when cancelling a Paid order).

**Status**: `RefundSalesOrderCommandHandler` calls `ISalesStockService.RestoreForReversalAsync(...)` after flipping status to `Refunded`. The cancel handler does the inverse for paid orders. The historical "refund leaves stock decremented" gap is closed.

**Edge**: same single-transaction caveat as F-2 — order status flips before the stock restore, and a transient failure in between leaves the two out of sync. No workaround needed under normal conditions.

### F-4. `POST /api/auth/register` silently ignores `role: "SuperAdmin"`

**Affected**: `POST /api/auth/register`.

**Where it breaks**: empirically, sending a register payload with `"role": "SuperAdmin"` returns `201` with `systemRole: "User"` in the response body, and the user row in the DB carries `SystemRole = 1` (User). Logging back in confirms the user is **not** a SuperAdmin. `RegisterCommandHandler` has the `Enum.TryParse` branch that *should* set `SuperAdmin`, so the cause is somewhere between request binding and persistence — but the observable behaviour is: register cannot mint a SuperAdmin.

**Impact**: this *accidentally* mitigates the security gap described in G-3 — but you can't rely on it. It also means there is **no** unattended way to bootstrap the very first SuperAdmin. The seeded `admin@possystem.com` account documented in [01 — Authentication](./01-authentication.md) is created by `UserSeeder.SeedAsync()`, which is only invoked via `POST /api/admin/seed-users` — itself gated behind `AdminOnly`. Chicken-and-egg.

**Workaround for bootstrapping**: insert (or `UPDATE`) the SuperAdmin row directly in the DB. Connection string is in `appsettings.Development.json`. The `SystemRole` column uses the enum's int value: `0 = SuperAdmin`, `1 = User`.

```sql
UPDATE "Users" SET "SystemRole" = 0 WHERE "Username" = '<your-bootstrap-user>';
```

**Fix when ready**: either (a) fix register to honour `role` and gate it behind a "must already be SuperAdmin" check (closes G-3 properly), or (b) add a one-shot bootstrap CLI command, or (c) remove the `role` field from the request DTO entirely so the docs stop promising something the endpoint won't deliver.

### F-5. Date-only payloads crash with `Cannot write DateTime with Kind=Unspecified`

**Affected**: any endpoint that accepts a date field — confirmed on `POST /api/purchaseorders` (`expectedDeliveryDate`) and `POST /api/purchaseorders/{id}/receive` (`expiryDate`). Likely also `POST /api/inventory/shops/{shopId}/stock`, `mark-paid.paidAt`, etc.

**Where it breaks**: `expectedDeliveryDate: "2026-05-15"` is parsed by `System.Text.Json` into a `DateTime` with `Kind = Unspecified`. The handler stores it on the entity as-is; on `SaveChangesAsync`, Npgsql refuses:

```
Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone',
only UTC is supported.
```

**Impact**: every example in this guide that uses a date-only literal (e.g. `"2026-05-15"`, `"2028-05-01"`) returns `500` against a real Postgres backend.

**Workaround**: send all date fields as ISO-8601 with an explicit UTC suffix — `"2026-05-15T00:00:00Z"` — until the handlers normalise to UTC themselves.

**Fix when ready**: in each command handler that touches a `DateTime` from the request, call `DateTime.SpecifyKind(value, DateTimeKind.Utc)` (or convert explicitly with `.ToUniversalTime()`). Or change the DTOs to `DateTimeOffset`.

### F-6. `GET /api/pdf/receipt/{orderId}` only matches `orderNumber`, not the order's id

**Affected**: `GET /api/pdf/receipt/{orderId}`.

**Where it breaks**: the route parameter is named `{orderId}` and the docs say "for a real sales order", but the handler resolves the value against `SalesOrder.OrderNumber` (e.g. `SO-20260508063819-7292`), not `SalesOrder.Id` (the GUID). Passing the order's `id` from `POST /api/salesorders` returns `404 Order ... not found`.

**Workaround**: pass `orderNumber` in the URL, not `id`.

**Fix when ready**: have the handler look up by both fields, or rename the route parameter to `{orderNumber}` and update this guide.

### F-7. `GET /api/inventory/shops/{shopId}/pos-items/by-barcode/{barcode}` 404s for stocked drugs

**Affected**: `GET /api/inventory/shops/{shopId}/pos-items/by-barcode/{barcode}`.

**Symptom (observed)**: a drug with a stored barcode and an active stock batch in the target shop returns `404 Item with barcode '…' not found or out of stock in this shop` from this endpoint, even though the barcode **does** match the drug and the inventory listing endpoint shows positive stock for the same `(shopId, drugId)`.

**Impact**: the till's barcode-scan flow described in [06 — Cashier](./06-cashier-pos-checkout.md) Step 0 cannot resolve scans. Cashiers must look the drug up by name or `drugId` instead.

**Workaround**: combine `GET /api/barcodes/search?barcode=…` (resolves barcode → drug) with `GET /api/inventory/shops/{shopId}` (filter to that drug for stock + price) until the handler is corrected.

**Fix when ready**: investigate the join — likely either case-/whitespace-sensitive barcode comparison or a filter that excludes batches whose `Location` is `Storage` (i.e. before any `move-to-floor`).

---

## Behavioural quirks

### Q-1. Several DTO fields are *repurposed* by `create-own` shop

In `POST /api/shops/create-own`, the request fields `taxId` and `description` are stored in non-obvious places:
- `taxId` → `vatRegistrationNumber`
- `description` → `pharmacyRegistrationNumber`, **truncated to 100 chars**

If the omitted `licenseNumber` is missing, a placeholder `TEMP-{8-char-guid}` is generated.

### Q-2. `Drug.categoryName` is a snapshot

`Drug.categoryName` is denormalised onto the drug at create time. Renaming the category later does not propagate. Use `categoryId` for joins; treat `categoryName` as historical display.

### Q-3. `ScanResultDto` doesn't resolve to a drug

`POST /api/barcodes/scan` returns only `{ success, data, message }`. The decoded barcode value is in `data` — you must follow up with `GET /api/barcodes/search?barcode=<data>` to identify the drug, or `GET /api/inventory/.../pos-items/by-barcode/{barcode}` for till-flavoured info (price + stock).

### Q-4. `PurchaseOrder.list` and `SalesOrder.list` use `pageSize`, others use `limit`

Inconsistent pagination param name across the API: most endpoints use `?page=&limit=`, but PO and Sales listings use `?page=&pageSize=`. Centralise in your client API layer.

### Q-5. `/inventory/shops/{shopId}/value` hard-codes USD

The endpoint returns `{ shopId, totalValue, currency: "USD" }` regardless of the shop's actual `currency` field. Convert client-side using the shop record.

### Q-6. `PUT /reduce` is not application-locked against concurrent calls

Two simultaneous sales of the last unit of a drug can both succeed and drive stock negative. The handler doesn't take a row lock. Mitigate at DB level (`SELECT … FOR UPDATE` in the repository).

### Q-7. `RegisterRequestDto.Role` is effectively ignored

The DTO defaults to the literal `"Staff"`, which doesn't match any value in the `SystemRole` enum, so the handler silently falls back to `User`. **Even when you explicitly pass `"SuperAdmin"`, the user is still saved as `User`** — see F-4 for details. In practice, the value of this field on a register call has no observable effect; treat it as an unused legacy field.

### Q-8. `StockCount.systemQuantity` is captured at schedule-time

If you `POST /stock-counts/shops/{shopId}` today and `record` tomorrow, the `systemQuantity` baseline reflects today's value. Sales between schedule and record are *not* in the variance — they're double-counted as shrinkage.

### Q-9. PDF receipt defaults are pharmacy-specific

`ReceiptDto` defaults: `currency = "IQD"`, `paperType = A5`, `language = "en-US"`. These are baked into the DTO's property defaults, not driven by the shop's config — pass them explicitly.

### Q-10. `RefreshToken` rotation invalidates old token

Every `/refresh` call mints a new refresh token *and* invalidates the old one. If the client stores the refresh token in two places that don't update atomically, one location will start failing.

### Q-11. ID prefix scheme is inconsistent across entities

The guides describe IDs as friendly prefixes — `USER-…`, `PO-…`, `SO-…`, `SHOP-…`, `DRG-…`, etc. In practice:

| Entity | Actual id format on create |
|--------|----------------------------|
| `User` | raw GUID (`2cb653f1-abc3-4b04-…`) |
| `PurchaseOrder` | raw GUID; `orderNumber` carries the `PO-…YYYYMMDD…` form |
| `SalesOrder` | raw GUID; `orderNumber` carries the `SO-…YYYYMMDD…` form |
| `PurchaseOrderItem` / `SalesOrderItem` | raw GUID |
| `Shop`, `Drug`, `Category`, `Supplier`, `ShopInventory` | prefixed (`SHOP-…`, `DRG-…`, `CAT-…`, `SUP-…`, `INV-…`) |

When code in this guide refers to `USER-9F3A2C1B`, `PO-9c8b7a6f`, or `SO-uuid` it's illustrative — your API will return GUIDs for those types. Cite `orderNumber` in human-readable contexts (receipts, search), but use `id` for state-transition routes (`/{id}/submit`, `/{id}/payment`) — except for `GET /api/pdf/receipt/{orderId}`, which is actually keyed on `orderNumber` (see F-6).

### Q-12. `lastLoginAt` is null in `/me` immediately after a fresh login

`LoginCommandHandler` calls `User.UpdateLastLogin()`, which mutates the entity in memory, but the change is not persisted in the same scope. A `GET /api/auth/me` issued one second later still returns `lastLoginAt: null`. The next login (after token refresh) does see the previous login's timestamp, suggesting `SaveChanges` is just on a different code path. Don't display "last seen" data sourced from `/me` — read it from a session log instead.

### Q-13. PO `paymentDueDate` is null until `/submit`

`POST /api/purchaseorders` returns `paymentDueDate: null` even when `paymentTerms = "Net30"`. The due date is only computed and stored when the PO transitions Draft → Submitted. If a UI shows the due date on a Draft, render it client-side from `orderDate + paymentTerms`.

### Q-14. `create-own` shop sets membership role to `Custom`, not `Owner`

`POST /api/shops/create-own` says it grants the caller `Role=Owner` with the canonical Owner permission set. Empirically the resulting `ShopUser.Role` comes back as `"Custom"` with the same broad permission list. `IsOwner` is correctly `true`, and the per-shop authorisation policies still pass — but client code that switches on `role === "Owner"` will miss self-created shops. Either match on `isOwner === true` or add `"Custom"` to the allow-list.

### Q-15. `AddStock` ignores `reorderPoint` when the inventory row is created

Calling `POST /api/inventory/shops/{shopId}/stock` for the first time with `reorderPoint: 20` returns an inventory row with `reorderPoint: 50` (the hard-coded default). Subsequent updates apparently respect the value. Set the reorder point with a follow-up call after the initial AddStock until this is fixed.

---

## Missing endpoints / features

These are reasonable pharmacy needs that the API doesn't currently expose:

- **Customer entity**: `SalesOrder` accepts `customerId/Name/Phone` strings, but there's no `/api/customers` resource. Customer info is denormalised onto orders.
- **Edit / delete a drug**: only `POST /api/drugs` exists. No `PUT`, no `DELETE`. Catalog corrections require DB-level edits.
- **Per-shop drug deactivation**: closest available is zero stock or skipping `move-to-floor`. No first-class "this shop doesn't sell this drug" flag.
- **Backup endpoint**: the `BackupData` permission exists in the role enum but no endpoint implements it. Run backups outside the API.
- **Split tender**: `payment` accepts a single `paymentMethod`. Can't model "customer paid $20 cash + $30 card" except by encoding both in `paymentReference`.
- **Server-issued idempotency keys**: most state-changing endpoints aren't idempotent. A network blip mid-request leaves you uncertain.

---

## How to use this list

If you're building a client:
- **Sales-order stock movement is now automatic** (deduction on `/payment`, restoration on `/refund` and `/cancel`). The historical workaround of manually calling `/reduce` after `/complete` is no longer needed and will double-decrement if you keep it.
- **Apply the F-1 workaround** for purchase-order receipts of brand-new drugs in a shop until the AddAsync/UpdateAsync bug is fixed.
- **Send all dates as ISO-8601 UTC** (`"…T00:00:00Z"`) until F-5 is fixed.
- **Use `orderNumber` for the PDF receipt route** (F-6).
- **Plan around the missing features** above — they may need separate tickets.

If you're maintaining the API:
- **The security gaps (G-1 through G-5) should be the next merge.** They're one-line annotation changes.
- **F-1 is a one-line fix** — drop the redundant `UpdateAsync` after `AddAsync` in `ReceiveStockCommandHandler`. The handler intent (auto-create + add batch) is correct.
- **F-4 (register can't elevate)** needs a deliberate decision: lock the role field down, or wire it correctly with a "must already be SuperAdmin" guard. Either way, fix the docs in [01 — Authentication](./01-authentication.md).
- **The behavioural quirks are *documented behaviour* now.** Fixing them is a breaking change — coordinate with API consumers.

---

→ Back to [the index](./README.md).
