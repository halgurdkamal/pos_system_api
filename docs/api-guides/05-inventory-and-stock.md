# 5. Inventory & Stock Operations

**What it's for**: every per-shop number — quantity on hand, cost, selling price, batches, expiry, packaging overrides, alerts, reports. This is **the** layer the till reads from to know what to sell and at what price.

**Use this when**:
- Onboarding a new product onto a shop's shelves → `POST /shops/{shopId}/stock`
- Setting/changing what a cashier charges → `PUT .../pricing` or `.../packaging-pricing`
- Replenishing the floor from back-room → `move-to-floor` / `move-to-storage`
- Manual quantity changes (damage, theft, returns) → `/api/stock-adjustments/*`
- Reconciling shelf vs system numbers → `/api/stock-counts/*`
- Moving stock between shops → `/api/stock-transfers/*`
- Reacting to low / expiring → `/api/inventory-alerts/*`
- Reports → `/api/inventory-reports/*`

**Where this fits**: the `Drug` from [03](./03-items-and-catalog.md) is the SKU; this guide is everything *physical* — what's actually in the building, who paid what for it, and what it's selling for today.

Each shop has a `ShopInventory` row per drug, holding multiple `Batch` value objects. Everything in this guide manipulates that state — viewing it, adjusting it, counting it, transferring it, and reacting to alerts.

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

### Inventory (base `/api/inventory`)

> ⚠ **Security note**: `InventoryController` is missing a class-level `[Authorize]`. Only the two cashier `pos-items` lookups carry `[Authorize(Policy = "ShopAccess")]`. **Every other endpoint here — including `AddStock`, `ReduceStock`, pricing changes — is currently reachable without a token.** Add `[Authorize(Policy = "ShopAccess")]` at the class level before deploying. Tracked in [`CONTROLLER_REFACTOR_BACKLOG`](../CONTROLLER_REFACTOR_BACKLOG.md).

| Method | Route | Purpose |
|--------|-------|---------|
| GET  | `/shops/{shopId}` | Paged list (`isAvailable=` optional) |
| GET  | `/shops/{shopId}/low-stock` | Below reorder point |
| GET  | `/shops/{shopId}/expiring?days=30` | Batches expiring soon |
| GET  | `/shops/{shopId}/value` | Total stock value — returns `{ "shopId", "totalValue", "currency": "USD" }` (currency is hard-coded in this endpoint) |
| GET  | `/shops/{shopId}/pos-items?searchTerm=&category=&page=&limit=50` | Cashier-friendly listing (`ShopAccess`) |
| GET  | `/shops/{shopId}/pos-items/by-barcode/{barcode}` | Single lookup for the till (`ShopAccess`) |
| POST | `/shops/{shopId}/stock` | Add a new batch (free-form, not via PO) |
| PUT  | `/shops/{shopId}/drugs/{drugId}/reduce` | Reduce stock (FIFO) |
| PUT  | `/shops/{shopId}/drugs/{drugId}/pricing` | Update shop pricing |
| PUT  | `/shops/{shopId}/drugs/{drugId}/reorder-point` | Change low-stock threshold |
| PUT  | `/shops/{shopId}/drugs/{drugId}/packaging-pricing` | Per-level pricing — body is a raw `{ "PKG-LV-…": 12.50, … }` dictionary keyed by `packagingLevelId` |
| GET  | `/shops/{shopId}/drugs/{drugId}/packaging-pricing` | Get per-level pricing |
| POST | `/shops/{shopId}/drugs/{drugId}/packaging-pricing/update-from-batch` | Auto-sync from active batch |
| POST | `/shops/{shopId}/drugs/{drugId}/move-to-floor` | Storage → ShopFloor |
| POST | `/shops/{shopId}/drugs/{drugId}/move-to-storage` | ShopFloor → Storage |
| GET  | `/shops/{shopId}/drugs/{drugId}/packaging` | Merged packaging (catalog + override) |
| POST | `/shops/{shopId}/drugs/{drugId}/packaging-overrides` | Create override |
| PUT  | `/shops/{shopId}/drugs/{drugId}/packaging-levels/{levelId}` | Tweak one level |

### Adjustments / Counts / Transfers / Alerts / Reports

| Base | Purpose |
|------|---------|
| `/api/stock-adjustments/shops/{shopId}` | Manual increments/decrements (damage, theft, returns) |
| `/api/stock-counts/shops/{shopId}` | Cycle counts & reconciliation |
| `/api/stock-transfers/shops/{fromShopId}` | Move stock between shops |
| `/api/inventory-alerts/shops/{shopId}` | Generate / list / acknowledge / resolve alerts |
| `/api/inventory-reports/shops/{shopId}` | Valuation, movement, ABC, expiry, turnover, dead-stock |

The Stock-Adjustments / Counts / Transfers / Alerts / Reports controllers all carry `[Authorize(Policy = "ShopAccess")]` at the class level — only the legacy `InventoryController` is missing it (see security note above).

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
      "drugId": "DRG-A1B2C3D4E5F6",
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

### B. Stock adjustment (write-off, damage, return)

```http
POST /api/stock-adjustments/shops/SHOP-AB12CD34

{
  "drugId": "DRG-A1B2C3D4E5F6",
  "batchNumber": "BATCH-2026-001",
  "adjustmentType": "Damage",
  "quantityChanged": -10,
  "reason": "Damaged in transit",
  "adjustedBy": "USER-9F3A2C1B",
  "notes": "Box 3 crushed",
  "referenceId": null,
  "referenceType": null
}
```

`adjustmentType` enum: `Damage`, `Return`, `StockWriteOff`, `Theft`, `Correction`. Negative `quantityChanged` reduces stock; positive increases.

Response captures `quantityBefore` / `quantityAfter` for the audit log:

```json
{
  "id": "ADJ-…",
  "drugId": "DRG-A1B2C3D4E5F6",
  "batchNumber": "BATCH-2026-001",
  "quantityChanged": -10,
  "quantityBefore": 500,
  "quantityAfter": 490,
  "adjustedAt": "2026-05-08T11:00:00Z"
}
```

### C. Stock count (cycle count)

Three calls, one count:

```http
# 1. Schedule
POST /api/stock-counts/shops/SHOP-AB12CD34
{
  "drugId": "DRG-A1B2C3D4E5F6",
  "scheduledAt": "2026-05-08T14:00:00Z",
  "notes": "Monthly cycle"
}
→ { "id": "COUNT-…", "status": "Scheduled", "systemQuantity": 490 }

# 2. Record what you actually counted
POST /api/stock-counts/COUNT-…/record
{
  "physicalQuantity": 485,
  "varianceReason": "Shrinkage/Loss"
}
→ varianceQuantity = 485 - 490 = -5 (auto-creates a StockAdjustment to reconcile)

# 3. Finalise
POST /api/stock-counts/COUNT-…/complete
→ { "status": "Completed", "completedAt": "..." }
```

### D. Stock transfer between shops

Four-step approval workflow:

```http
# 1. From shop initiates
POST /api/stock-transfers/shops/SHOP-AB12CD34
{
  "toShopId": "SHOP-WXYZ9999",
  "drugId": "DRG-A1B2C3D4E5F6",
  "batchNumber": "BATCH-2026-001",
  "quantity": 50,
  "notes": "Restock request"
}
→ status: Pending  (sender's stock reduced, TransferOut adjustment recorded)

# 2. Manager approves
POST /api/stock-transfers/{transferId}/approve
→ status: Approved → InTransit

# 3. Receiver acknowledges arrival
POST /api/stock-transfers/{transferId}/receive
→ status: Completed (stock added to receiver's inventory)

# 4. Or cancel before receive
POST /api/stock-transfers/{transferId}/cancel
{ "reason": "No longer needed" }
→ status: Cancelled (sender's stock restored)
```

### E. Inventory alerts

Alerts are **generated on demand**, not in a background job — call `/generate` from a cron, on shop load, or after big inventory changes.

```http
# Generate (scans for low stock + expiring batches)
POST /api/inventory-alerts/shops/SHOP-AB12CD34/generate
→ { "newAlertsCreated": 4 }

# List
GET /api/inventory-alerts/shops/SHOP-AB12CD34?severity=Critical&alertType=LowStock
→ array of { alertType, severity, status, message,
             currentQuantity, thresholdQuantity, expiryDate }

# Summary widget
GET /api/inventory-alerts/shops/SHOP-AB12CD34/summary
→ { totalActive, criticalCount, warningCount, infoCount, alertTypeBreakdown }

# Workflow
POST /api/inventory-alerts/{alertId}/acknowledge   → status: Acknowledged
POST /api/inventory-alerts/{alertId}/resolve
{ "resolutionNotes": "Reordered 500 units (PO-…)" }
→ status: Resolved, resolvedAt, resolutionNotes saved
```

### F. Reports

| Report | Endpoint | Notable params | Returns |
|--------|----------|---------------|---------|
| Valuation | `/api/inventory-reports/shops/{shopId}/valuation` | — | `totalItems`, `totalUnits`, `totalValue`, items |
| Movement | `/movement` | `fromDate`, `toDate`, `drugId` | adjustment history with before/after qtys |
| ABC analysis | `/abc-analysis` | — | items split A (top 70% value) / B (20%) / C (10%) |
| Expiry | `/expiry` | `daysAhead=90` | counts of expired + 30/60/90-day buckets |
| Turnover | `/turnover` | `fromDate`, `toDate` | `totalSold`, `averageStock`, `turnoverRate`, `daysOfSupply` |
| Dead stock | `/dead-stock` | `daysThreshold=180` | items with no movement for N days |

---

## How FIFO works on outflows

Every reduction (sale, transfer-out, write-off without batch) flows through `ShopInventory.ReduceStock(qty)`:

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

**Move-to-floor uses FEFO** (`OrderBy(ExpiryDate)`) — push the soonest-expiring stock to the till first.

## Common pitfalls

- **Negative stock** — handlers reject reductions exceeding `totalStock`; the cashier flow checks first via `/pos-items/by-barcode/{barcode}`.
- **Adjustments aren't sales** — they bypass FIFO selection (you must pass `batchNumber`). Use sale endpoints to deduct via FIFO.
- **Transfers don't auto-receive** — the receiving shop's stock only goes up after `POST /receive`. Stock is in limbo (InTransit) before that.

## Next

→ [06 — Cashier / POS Checkout](./06-cashier-pos-checkout.md)
