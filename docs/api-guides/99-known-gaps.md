# 99. Known Gaps

A single canonical list of issues in the API today. Each item names the affected endpoint(s), the impact, and the workaround your client must apply until the underlying handler is fixed.

The other guides reference this page instead of duplicating the warnings inline.

## Contents

- [Recently closed](#recently-closed)
- [Critical security gaps](#critical-security-gaps)
- [Functional TODOs (handlers that don't fully do what their name suggests)](#functional-todos)
- [Behavioural quirks (working as built, but surprising)](#behavioural-quirks)
- [Missing endpoints / features](#missing-endpoints--features)

---

## Recently closed

These were live gaps in earlier revisions of this guide. The fixes have landed on `main` — included here so older client code doing the documented workaround can be cleaned up.

| Tag | What changed | Commit |
|-----|--------------|--------|
| **G-1** | `InventoryController` now has class-level `[Authorize(Policy = "ShopAccess")]`. Anonymous access to `AddStock`/`ReduceStock`/pricing is closed. | `5acc1fa` |
| **G-2** | `PUT /api/shops/{id}/receipt-config` and `…/hardware-config` are gated by `ShopOwnerOrAdmin`. | `40f6556` |
| **G-3 / F-4** | The `role` field on `POST /api/auth/register` was removed end-to-end (DTO, command, handler). The endpoint now always creates `SystemRole.User`. Bootstrap a SuperAdmin via direct DB update. | `0832062` |
| **G-4** | `POST /api/barcodes/generate/barcode` and `/qrcode` require `[Authorize]`. | `7ade694` |
| **G-5** | `POST /api/pdf/receipt/custom` is no longer `[AllowAnonymous]`. `logoUrl` is rejected unless it's an absolute http(s) URL pointing to a public host (loopback, RFC1918, link-local, and cloud metadata IPs are blocked). | `0cbf902` |
| **F-1** | PO `/receive` no longer crashes on the first batch for a `(shopId, drugId)` pair. The handler now skips `UpdateAsync` when the inventory row was just `AddAsync`-ed. | `fac79a2` |
| **F-5** | A global JSON converter coerces every incoming `DateTime` (and `DateTime?`) to `DateTimeKind.Utc`, so date-only literals like `"2026-05-15"` no longer crash with `Cannot write DateTime with Kind=Unspecified`. **You can drop the `T00:00:00Z` suffix workaround in client code.** | `d44f3df` |
| **F-6** | `GET /api/pdf/receipt/{orderId}` now resolves the path parameter against either `SalesOrder.Id` or `SalesOrder.OrderNumber`. | `6f50769` |
| **Q-5** | `GET /api/inventory/shops/{shopId}/value` returns the shop's actual `currency`, not hard-coded USD. | `f59b28b` |
| **Q-12** | Successful login now persists `LastLoginAt` via a focused `UpdateLoginInfoAsync` that doesn't re-attach the `ShopMemberships` graph. `GET /api/auth/me` reflects the timestamp on the very next call. | `a0414ec` |
| **Q-14** | `POST /api/shops/create-own` returns `role: "Owner"` (not `"Custom"`). The fix removes an EF Core `HasDefaultValue(ShopRole.Custom)` collision with `Owner = 0` (CLR default for `int`) and routes role assignment through `SetRole(ShopRole.Owner)` for canonical permissions. | `ce2c1d5` |
| **Q-15** | `POST /api/inventory/shops/{shopId}/stock` accepts `reorderPoint` on the wire (it was missing from `AddStockDto`). The first-create path honours the supplied value. | `ba3e4d7` |
| **F-7** | `GET /api/inventory/shops/{shopId}/pos-items/by-barcode/{barcode}` now resolves the inventory join against either `drug.DrugId` (prefixed, from AddStock/CreateDrug) or `drug.Id` (raw GUID PK, from PO `/receive` paths) — matching the listing endpoint's dual-key behaviour. | (this session) |

---

## Critical security gaps

*(none open at the moment — see the **Recently closed** table above)*

---

## Functional TODOs

### F-2. Sales `/payment` deduction is not transactional with the status flip

**Affected**: `POST /api/salesorders/{id}/payment`.

**Status**: deduction is wired up — `ProcessPaymentCommandHandler` calls `ISalesStockService.DeductForSaleAsync(...)` immediately after flipping status `Confirmed → Paid`. The historical "complete doesn't deduct" gap is closed.

**Edge**: if `/payment` succeeds in flipping the order's status but the subsequent inventory call fails (e.g. transient DB error), the order is `Paid` while inventory still reads the old level. The two writes are not yet wrapped in a single DB transaction. Until they are, surface payment failures explicitly in the till and add a reconciliation script.

**Fix when ready**: wrap status flip + `DeductForSaleAsync` in a UoW transaction (`IUnitOfWork` already exists per commit `35bc6a8`).

### F-3. Refund / cancel restoration shares F-2's transaction edge

**Affected**: `POST /api/salesorders/{id}/refund`, `POST /api/salesorders/{id}/cancel` (when cancelling a `Paid` order).

**Status**: `RefundSalesOrderCommandHandler` calls `ISalesStockService.RestoreForReversalAsync(...)` after flipping status to `Refunded`. The cancel handler does the inverse for paid orders. The historical "refund leaves stock decremented" gap is closed.

**Edge**: same single-transaction caveat as F-2 — order status flips before the stock restore. A transient failure in between leaves the two out of sync. No workaround needed under normal conditions; fix in lockstep with F-2.

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

### Q-6. `PUT /reduce` is not application-locked against concurrent calls

Two simultaneous sales of the last unit of a drug can both succeed and drive stock negative. The handler doesn't take a row lock. Mitigate at DB level (`SELECT … FOR UPDATE` in the repository).

### Q-7. ~~`RegisterRequestDto.Role` is effectively ignored~~ *(closed)*

The `role` field has been removed from the register DTO. Tracked in the **Recently closed** table as G-3 / F-4.

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

When code in this guide refers to `USER-9F3A2C1B`, `PO-9c8b7a6f`, or `SO-uuid` it's illustrative — your API will return GUIDs for those types. Cite `orderNumber` in human-readable contexts (receipts, search), but use `id` for state-transition routes (`/{id}/submit`, `/{id}/payment`). The PDF receipt route accepts either form (see the F-6 entry in the **Recently closed** table).

### Q-13. PO `paymentDueDate` is null until `/submit`

`POST /api/purchaseorders` returns `paymentDueDate: null` even when `paymentTerms = "Net30"`. The due date is only computed and stored when the PO transitions Draft → Submitted. If a UI shows the due date on a Draft, render it client-side from `orderDate + paymentTerms`.

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
- **Sales-order stock movement is automatic** (deduction on `/payment`, restoration on `/refund` and `/cancel`). The historical workaround of manually calling `/reduce` after `/complete` is no longer needed and will double-decrement if you keep it.
- **Date fields accept either form** — `"2026-05-15"` or `"2026-05-15T00:00:00Z"` both work now (F-5 closed). Server normalises to UTC.
- **PDF receipt route accepts either `id` or `orderNumber`** (F-6 closed). New code should use `id` for consistency with other state-transition routes.
- **Plan around the missing features** above — they may need separate tickets.

If you're maintaining the API:
- **Wrap payment / refund / cancel + stock writes in a single UoW transaction** (closes the F-2 / F-3 transaction edge).
- **Q-6 (`/reduce` row lock)** is a real correctness bug despite the "quirk" classification — concurrent last-unit sales can drive stock negative.
- **The behavioural quirks are *documented behaviour* now.** Fixing them is a breaking change — coordinate with API consumers.

---

→ Back to [the index](./README.md).
