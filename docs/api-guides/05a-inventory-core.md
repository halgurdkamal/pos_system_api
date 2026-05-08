# 5a. Inventory — Core (view, stock, pricing, packaging)

**What it's for**: the day-to-day per-shop state — what's on the shelf, what it costs, what it sells for, where it lives in the building, and what packaging levels customers can buy.

**Use this when**:
- Looking up a drug's current stock or showing the catalog page → `GET /shops/{shopId}/...`
- Onboarding a new product onto a shop's shelves → `POST /shops/{shopId}/stock`
- Setting/changing what a cashier charges → `PUT .../pricing` or `.../packaging-pricing`
- Replenishing the floor from back-room → `move-to-floor` / `move-to-storage`

**Use the sibling guide [`05b — Stock Operations`](./05b-stock-operations.md) when** you need adjustments, counts, transfers, alerts, or reports.

## Contents

- [How stock is structured](#how-stock-is-structured)
- [Endpoint summary](#endpoint-summary)
- [Step-by-step flows](#step-by-step-flows)
  - [A. View current stock for a shop](#a-view-current-stock-for-a-shop)
  - [B. Add stock (create a batch)](#b-add-stock-create-a-batch)
  - [C. Update prices](#c-update-prices)
  - [D. Move between storage and shop floor](#d-move-between-storage-and-shop-floor)
  - [E. Per-shop packaging overrides](#e-per-shop-packaging-overrides)
- [How FIFO works on outflows](#how-fifo-works-on-outflows)
- [Best practices](#best-practices)

## How stock is structured

```
ShopInventory (per shop+drug)
├── totalStock             ← sum of all active batches
├── shopFloorStock         ← on display, ready for cashier
├── storageStock           ← back-room, not yet on the floor
├── reservedStock          ← held for pending sales
├── quarantinedStock       ← damaged / under recall, not sellable
├── reorderPoint           ← triggers low-stock alerts
├── shopPricing            ← cost / selling / tax / currency
├── packagingOverride      ← per-shop packaging tweaks
└── batches[]              ← FIFO consumed oldest receivedDate first
       ├── batchNumber, supplierId
       ├── quantityOnHand
       ├── receivedDate, expiryDate
       ├── purchasePrice, sellingPrice
       ├── status: Active | Expired | Recalled | Reserved
       └── location: Storage | ShopFloor | Reserved | Quarantine
```

## Endpoint summary

All endpoints in this controller require `[Authorize(Policy = "ShopAccess")]` — the caller must hold a token with access to the target `shopId` (commit `5acc1fa`, closing G-1).

| Method | Route | Purpose |
|--------|-------|---------|
| GET  | `/shops/{shopId}` | Paged list (`isAvailable=` optional) |
| GET  | `/shops/{shopId}/low-stock` | Below reorder point |
| GET  | `/shops/{shopId}/expiring?days=30` | Batches expiring soon |
| GET  | `/shops/{shopId}/value` | Total stock value — returns `{ "shopId", "totalValue", "currency" }`; `currency` reflects the shop's configured currency |
| GET  | `/shops/{shopId}/pos-items?searchTerm=&category=&page=&limit=50` | Cashier-friendly listing (`ShopAccess`) |
| GET  | `/shops/{shopId}/pos-items/by-barcode/{barcode}` | Single till lookup (`ShopAccess`) |
| POST | `/shops/{shopId}/stock` | Add a new batch (free-form, not via PO) |
| PUT  | `/shops/{shopId}/drugs/{drugId}/reduce` | Reduce stock (FIFO) |
| PUT  | `/shops/{shopId}/drugs/{drugId}/pricing` | Update flat shop pricing (legacy) |
| PUT  | `/shops/{shopId}/drugs/{drugId}/reorder-point` | Change low-stock threshold |
| PUT  | `/shops/{shopId}/drugs/{drugId}/packaging-pricing` | Per-level pricing — body is `{ "PKG-LV-…": 12.50, … }` keyed by `packagingLevelId` |
| GET  | `/shops/{shopId}/drugs/{drugId}/packaging-pricing` | Get per-level pricing |
| POST | `/shops/{shopId}/drugs/{drugId}/packaging-pricing/update-from-batch` | Auto-sync from active batch |
| POST | `/shops/{shopId}/drugs/{drugId}/move-to-floor` | Storage → ShopFloor |
| POST | `/shops/{shopId}/drugs/{drugId}/move-to-storage` | ShopFloor → Storage |
| GET  | `/shops/{shopId}/drugs/{drugId}/packaging` | Merged packaging (catalog + override) |
| POST | `/shops/{shopId}/drugs/{drugId}/packaging-overrides` | Create override |
| PUT  | `/shops/{shopId}/drugs/{drugId}/packaging-levels/{levelId}` | Tweak one level |

---

## Step-by-step flows

### A. View current stock for a shop

```http
GET /api/inventory/shops/SHOP-AB12CD34?page=1&limit=20
```

Response (paginated):

```json
{
  "items": [
    {
      "id": "INV-…",
      "shopId": "SHOP-AB12CD34",
      "drugId": "DRG-A1B2C3D4",
      "totalStock": 500,
      "shopFloorStock": 50,
      "storageStock": 450,
      "reservedStock": 0,
      "quarantinedStock": 0,
      "reorderPoint": 50,
      "isAvailable": true,
      "shopPricing": {
        "costPrice": 5.50,
        "sellingPrice": 12.00,
        "taxRate": 0.10,
        "currency": "USD"
      },
      "batches": [
        {
          "batchNumber": "BATCH-2026-001",
          "quantityOnHand": 500,
          "receivedDate": "2026-05-01T00:00:00Z",
          "expiryDate": "2028-05-01T00:00:00Z",
          "purchasePrice": 5.50,
          "sellingPrice": 12.00,
          "status": "Active",
          "location": "Storage"
        }
      ]
    }
  ],
  "page": 1, "limit": 20, "total": 1
}
```

Quick-glance variants:
- `GET /shops/{shopId}/low-stock` — rows where `totalStock < reorderPoint`.
- `GET /shops/{shopId}/expiring?days=30` — rows with at least one batch in the window.
- `GET /shops/{shopId}/value` — `{ shopId, totalValue, currency }` (currency comes from the shop record).

### B. Add stock (create a batch)

This is the **bridge between the catalog and the shelf**. It lazily creates a `ShopInventory` row the first time, then appends a `Batch`.

```http
POST /api/inventory/shops/SHOP-AB12CD34/stock

{
  "drugId":         "DRG-A1B2C3D4",
  "supplierId":     "SUP-456def",
  "batchNumber":    "BATCH-2026-001",
  "quantity":       500,
  "expiryDate":     "2028-05-01",
  "purchasePrice":  5.50,
  "sellingPrice":   12.00,
  "storageLocation":"Shelf A-3",
  "reorderPoint":   50
}
```

Server-side rules (from `AddStockCommandHandler`):
- `drugId` must exist (`Drug` table) — else `400 "Drug … does not exist"`.
- `supplierId` must exist — else `400 "Supplier … does not exist"`.
- If no `ShopInventory` exists for `(shopId, drugId)`, one is created with the given values, plus defaults: `currency = "USD"`, `taxRate = 0`, `reorderPoint = 50` if `reorderPoint` is omitted (the field is honoured on first create — Q-15 closed in `ba3e4d7`).
- The `Batch` is always added with `status = Active`, `location = Storage`, `receivedDate = now`.

Returns the full `InventoryDto` with the new batch in `batches[]`.

### C. Update prices

Two endpoints. **Use packaging-pricing for new code; `pricing` is the legacy flat model.**

```http
# Legacy: flat cost / selling / tax. Affects the till's default selling price.
PUT /api/inventory/shops/SHOP-AB12CD34/drugs/DRG-A1B2C3D4/pricing
{ "costPrice": 5.50, "sellingPrice": 12.00, "taxRate": 0.10 }

# Modern: per-packaging-level prices. The till reads these.
PUT /api/inventory/shops/SHOP-AB12CD34/drugs/DRG-A1B2C3D4/packaging-pricing
{
  "PKG-LV-…strip…": 1.50,
  "PKG-LV-…box…":   12.00
}

# Auto-fill packaging prices from the active batch's purchase price (preserves any
# custom prices you've already set; only fills nulls/zeros)
POST /api/inventory/shops/SHOP-AB12CD34/drugs/DRG-A1B2C3D4/packaging-pricing/update-from-batch
```

`packaging-pricing` body keys are the `packagingLevelId` strings — fetch them from `GET /shops/{shopId}/drugs/{drugId}/packaging`.

### D. Move between storage and shop floor

The till sells from `shopFloorStock`. Stock starts in `storageStock` (where `AddStock` lands it) and is moved to the floor as needed.

```http
POST /api/inventory/shops/SHOP-AB12CD34/drugs/DRG-A1B2C3D4/move-to-floor
{ "quantity": 50, "batchNumber": null }
```

`batchNumber: null` ⇒ **FEFO** (oldest expiry first). Pass an explicit `batchNumber` to move from a specific lot.

```http
POST /api/inventory/shops/SHOP-AB12CD34/drugs/DRG-A1B2C3D4/move-to-storage
{ "quantity": 30, "batchNumber": "BATCH-2026-001" }
```

Both endpoints return the updated `InventoryDto`.

### E. Per-shop packaging overrides

The catalog defines a default packaging hierarchy on the `Drug`. A shop can deviate (e.g. disable loose-tablet sales).

```http
GET  /api/inventory/shops/{shopId}/drugs/{drugId}/packaging         → merged hierarchy (catalog + override)
POST /api/inventory/shops/{shopId}/drugs/{drugId}/packaging-overrides → create override
PUT  /api/inventory/shops/{shopId}/drugs/{drugId}/packaging-levels/{levelId} → tweak one level
```

For the underlying domain model (when overrides apply, how levels are merged), see [`../packaging/system-guide.md`](../packaging/system-guide.md).

---

## How FIFO works on outflows

Every reduction (sale, transfer-out, adjustment without explicit batch) flows through `ShopInventory.ReduceStock(qty)`:

```csharp
var activeBatches = Batches
    .Where(b => b.Status == BatchStatus.Active && b.QuantityOnHand > 0)
    .OrderBy(b => b.ReceivedDate)        // oldest first = FIFO
    .ToList();

foreach (var batch in activeBatches)
{
    if (batch.QuantityOnHand >= remaining) {
        batch.QuantityOnHand -= remaining;
        remaining = 0;
        break;
    }
    remaining -= batch.QuantityOnHand;
    batch.QuantityOnHand = 0;
}
```

If 3 batches hold (100, 200, 150) and 250 units are sold, batch 1 is exhausted, 150 is taken from batch 2, batch 3 untouched. When the active selling batch flips to a new one, prices may auto-propagate — see [`../pricing/from-batch-guide.md`](../pricing/from-batch-guide.md).

**Move-to-floor uses FEFO** (`OrderBy(ExpiryDate)`) — push the soonest-expiring stock to the till first, even though depletion is FIFO.

---

## Best practices

### Security
- **Add `[Authorize(Policy = "ShopAccess")]` at the class level on `InventoryController`** before deploying. Today only the two `/pos-items*` endpoints are protected. This is the single most important fix in the whole API.
- **Pricing endpoints change what customers are charged.** Restrict further with the `UpdatePricing` permission so cashiers can't shift prices without authorisation.
- **Don't accept arbitrary `storageLocation` strings as searchable identifiers** — they're free-text. Use them for human display, not as a key.

### Performance
- **`Batches[]` grows unbounded.** Mark exhausted batches `Expired`/`Recalled` and archive periodically. A drug receiving weekly deliveries for 2 years has 100+ batch entries — every reduce/listing iterates them.
- **`/pos-items/by-barcode/{barcode}` is the hot till path.** Index `Drug.Barcode` and `(ShopInventory.ShopId, ShopInventory.DrugId)`.
- **`GET /shops/{shopId}` returns inventory + batches + pricing** — heavy. Use `low-stock` / `expiring` / `value` for dashboards.

### Correctness
- **`/reduce` runs FIFO**; `/move-to-floor` with `batchNumber: null` runs FEFO. Pick the right one — using FIFO when you mean FEFO ages near-expiry stock.
- **Concurrent `/reduce` calls are not application-locked.** Two near-simultaneous sales on the last unit can drive stock negative. Mitigate at DB level (`SELECT … FOR UPDATE`) and validate before reducing.
- **`/value` hard-codes USD** in the response. Convert client-side using the shop's actual currency.
- **One `ShopInventory` per `(shopId, drugId)`** — invariant. `AddStock` enforces it. Don't try to "split" by storage location; use `Location` on the `Batch`.

### Clean code
- **Prefer `/packaging-pricing` over `/pricing`** in new code. Per-level pricing is the modern model; flat pricing is for backward compat.
- **Use `update-from-batch`** instead of recomputing prices client-side when a new batch becomes active.
- **Treat `shopFloorStock` and `storageStock` as physical realities**, not numbers to balance. Reading `totalStock` for till availability hides why the cashier sees zero when there are pallets in the back.

---

## Next

→ [05b — Stock Operations (adjustments, counts, transfers, alerts, reports)](./05b-stock-operations.md)
→ [06 — Cashier / POS Checkout](./06-cashier-pos-checkout.md)
