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

### F-1. PO `/receive` does not create a `Batch` on `ShopInventory`

**Affected**: `POST /api/purchaseorders/{id}/receive`.

**Where it breaks**: `ReceiveStockCommandHandler.UpdateInventoryAsync` only logs the receipt — see the comment `// TODO: Integrate with StockAdjustment system to properly track batches, expiry dates, and update inventory totals`.

**Impact**: receiving a PO updates the PO record (status, `receipts[]`, `receivedQuantity`) but **does not** raise the shop's stock. The till sees no new stock.

**Workaround**: after every successful `/receive`, call `POST /api/inventory/shops/{shopId}/stock` with the same `batchNumber`, `quantity`, `expiryDate`, `purchasePrice`, and `supplierId`. See [Recipe 3 in the cookbook](./08-data-model-and-recipes.md#recipe-3--receive-stock-from-a-purchase-order-current-reality).

**Fix when ready**: complete `UpdateInventoryAsync` to call `IInventoryRepository` and add a `Batch`, then drop the workaround.

### F-2. Sales `/complete` does not deduct stock

**Affected**: `POST /api/salesorders/{id}/complete`.

**Where it breaks**: `CompleteSalesOrderCommandHandler` only flips the order status. It does **not** call `ReduceStockCommand` or otherwise touch `ShopInventory`.

**Impact**: completed sales don't deplete stock automatically — `totalStock` drifts upward over time, FIFO never kicks in, batch propagation never fires, low-stock alerts never trigger.

**Workaround**: after a successful `/complete`, the till must call `PUT /api/inventory/shops/{shopId}/drugs/{drugId}/reduce` once per item, using each line's `baseUnitsConsumed`. See [Recipe 4 in the cookbook](./08-data-model-and-recipes.md#recipe-4--daily-till-workflow-cashier-point-of-view).

**Fix when ready**: have `CompleteSalesOrderCommandHandler` iterate items and call the inventory reducer (ideally inside the same transaction).

### F-3. Refunds don't restore stock

**Affected**: `POST /api/salesorders/{id}/refund`.

**Where it breaks**: the refund handler flips status to `Refunded` and stamps `cancelledBy` + `cancellationReason`. It does not raise `ShopInventory`.

**Impact**: even if F-2 is fixed in the future, refunds will stay one-way unless this is also wired up.

**Workaround**: after a successful `/refund`, file a `POST /api/stock-adjustments/shops/{shopId}` with `adjustmentType: "Return"`, the affected `batchNumber`, positive `quantityChanged`, and `referenceId: <orderId>`, `referenceType: "SalesOrder"`.

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

### Q-7. `RegisterRequestDto.Role` defaults to `"Staff"`

`"Staff"` doesn't match any value in the `SystemRole` enum, so the handler silently falls back to `User`. Pass `"User"` explicitly in code you control to avoid future enum-change surprises.

### Q-8. `StockCount.systemQuantity` is captured at schedule-time

If you `POST /stock-counts/shops/{shopId}` today and `record` tomorrow, the `systemQuantity` baseline reflects today's value. Sales between schedule and record are *not* in the variance — they're double-counted as shrinkage.

### Q-9. PDF receipt defaults are pharmacy-specific

`ReceiptDto` defaults: `currency = "IQD"`, `paperType = A5`, `language = "en-US"`. These are baked into the DTO's property defaults, not driven by the shop's config — pass them explicitly.

### Q-10. `RefreshToken` rotation invalidates old token

Every `/refresh` call mints a new refresh token *and* invalidates the old one. If the client stores the refresh token in two places that don't update atomically, one location will start failing.

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
- **Apply the workarounds for F-1, F-2, F-3.** These are non-negotiable for correctness; without them stock counts will drift.
- **Plan around the missing features** above — they may need separate tickets.
- **Wrap the workarounds in a single helper** (e.g. `completeSaleAndDeductStock(orderId)`) so when handlers are fixed, you remove the band-aid in one place.

If you're maintaining the API:
- **The security gaps (G-1 through G-5) should be the next merge.** They're one-line annotation changes.
- **F-1 and F-2 require a transactional handler change** — design carefully (race conditions, partial failures). Reference [Recipes 3 and 4](./08-data-model-and-recipes.md#cross-api-recipes) for the expected behaviour.
- **The behavioural quirks are *documented behaviour* now.** Fixing them is a breaking change — coordinate with API consumers.

---

→ Back to [the index](./README.md).
