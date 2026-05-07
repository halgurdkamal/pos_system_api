# 5b. Stock Operations (adjustments, counts, transfers, alerts, reports)

**What it's for**: the supporting workflows around inventory — recording manual quantity changes, reconciling against physical counts, moving stock between shops, reacting to low/expiring alerts, and pulling reports.

**Use this when**:
- Damage / theft / mis-counts → `POST /api/stock-adjustments/shops/{shopId}`
- Cycle or full count → `/api/stock-counts/*`
- Transferring stock to another branch → `/api/stock-transfers/*`
- Surfacing low / expiring stock to staff → `/api/inventory-alerts/*`
- Management dashboards (valuation, turnover, ABC, dead stock) → `/api/inventory-reports/*`

For viewing live stock or adding new batches, see [`05a — Inventory Core`](./05a-inventory-core.md).

## Contents

- [Endpoint summary](#endpoint-summary)
- [A. Stock adjustment (damage, theft, return)](#a-stock-adjustment-damage-theft-return)
- [B. Stock count (cycle / full)](#b-stock-count-cycle--full)
- [C. Stock transfer between shops](#c-stock-transfer-between-shops)
- [D. Inventory alerts](#d-inventory-alerts)
- [E. Reports](#e-reports)
- [Best practices](#best-practices)

## Endpoint summary

All endpoints in this guide carry `[Authorize(Policy = "ShopAccess")]` at the controller class level.

| Base | Purpose |
|------|---------|
| `/api/stock-adjustments/shops/{shopId}` | Manual increments/decrements (damage, theft, returns, corrections) |
| `/api/stock-counts/shops/{shopId}` | Cycle counts and reconciliation |
| `/api/stock-transfers/shops/{fromShopId}` | Move stock between shops with approval workflow |
| `/api/inventory-alerts/shops/{shopId}` | Generate / list / acknowledge / resolve alerts |
| `/api/inventory-reports/shops/{shopId}` | Valuation, movement, ABC, expiry, turnover, dead-stock |

---

## A. Stock adjustment (damage, theft, return)

A `StockAdjustment` is the **audit row** for any manual quantity change. Every change should produce one — they're how the compliance trail is reconstructed.

```http
POST /api/stock-adjustments/shops/SHOP-AB12CD34

{
  "drugId":         "DRG-A1B2C3D4",
  "batchNumber":    "BATCH-2026-001",
  "adjustmentType": "Damage",
  "quantityChanged": -10,
  "reason":          "Damaged in transit",
  "adjustedBy":      "USER-9F3A2C1B",
  "notes":           "Box 3 crushed",
  "referenceId":     null,
  "referenceType":   null
}
```

`adjustmentType` enum: `Damage`, `Return`, `StockWriteOff`, `Theft`, `Correction`. **Negative `quantityChanged` reduces** stock; positive increases.

Response captures `quantityBefore` / `quantityAfter` for the audit log:

```json
{
  "id":              "ADJ-…",
  "drugId":          "DRG-A1B2C3D4",
  "batchNumber":     "BATCH-2026-001",
  "quantityChanged": -10,
  "quantityBefore":  500,
  "quantityAfter":   490,
  "adjustedAt":      "2026-05-08T11:00:00Z"
}
```

`referenceId` / `referenceType` link the adjustment to the *cause*, e.g. a refunded `SalesOrder` that's restoring stock. Use `referenceType: "SalesOrder"`, `referenceId: "<orderId>"`.

```http
GET /api/stock-adjustments/shops/SHOP-AB12CD34?fromDate=…&toDate=…
GET /api/stock-adjustments/shops/SHOP-AB12CD34/drugs/DRG-A1B2C3D4
```

---

## B. Stock count (cycle / full)

A three-call workflow: schedule → record → complete.

```http
# 1. Schedule — captures systemQuantity at this moment as the baseline
POST /api/stock-counts/shops/SHOP-AB12CD34
{
  "drugId":     "DRG-A1B2C3D4",
  "scheduledAt":"2026-05-08T14:00:00Z",
  "notes":      "Monthly cycle"
}
→ { "id": "COUNT-…", "status": "Scheduled", "systemQuantity": 490 }

# 2. Record — what staff actually counted on the shelf
POST /api/stock-counts/COUNT-…/record
{
  "physicalQuantity": 485,
  "varianceReason":   "Shrinkage/Loss"
}
→ varianceQuantity = 485 - 490 = -5
   (auto-creates a StockAdjustment to reconcile the system to physical)

# 3. Finalise
POST /api/stock-counts/COUNT-…/complete
→ { "status": "Completed", "completedAt": "..." }

# Listing
GET /api/stock-counts/shops/SHOP-AB12CD34?status=Scheduled
```

Status enum: `Scheduled`, `InProgress`, `Completed`, `Cancelled`. `systemQuantity` is captured at schedule-time and **doesn't update** if stock changes before the count.

---

## C. Stock transfer between shops

Four-step approval workflow. Stock leaves the sender at *initiation*, arrives at the receiver at *receive*. Cancellation rules differ depending on which step you're at.

```http
# 1. From shop initiates — sender's stock decremented immediately
POST /api/stock-transfers/shops/SHOP-AB12CD34
{
  "toShopId":    "SHOP-WXYZ9999",
  "drugId":      "DRG-A1B2C3D4",
  "batchNumber": "BATCH-2026-001",
  "quantity":    50,
  "notes":       "Restock request"
}
→ status: Pending  (TransferOut adjustment recorded on sender)

# 2. Manager approves
POST /api/stock-transfers/{transferId}/approve
→ status: Approved → InTransit

# 3. Receiver acknowledges arrival — receiver's stock incremented
POST /api/stock-transfers/{transferId}/receive
→ status: Completed

# Cancel before /receive — sender's reduction is reversed
POST /api/stock-transfers/{transferId}/cancel
{ "reason": "Truck broke down" }
→ status: Cancelled

# Listings
GET /api/stock-transfers/shops/{shopId}/pending
GET /api/stock-transfers/shops/{shopId}/history
```

`shopId` in the listing endpoints matches transfers where the shop is either sender or receiver.

---

## D. Inventory alerts

Alerts are **generated on demand**, not from a background job. Run `/generate` from a cron, on shop load, or after big inventory changes.

```http
# Generate (scans for low stock + expiring batches)
POST /api/inventory-alerts/shops/SHOP-AB12CD34/generate
→ { "newAlertsCreated": 4 }

# List
GET /api/inventory-alerts/shops/SHOP-AB12CD34?severity=Critical&alertType=LowStock
→ array of {
    alertType, severity, status, message,
    currentQuantity, thresholdQuantity, expiryDate
  }

# Summary widget
GET /api/inventory-alerts/shops/SHOP-AB12CD34/summary
→ { totalActive, criticalCount, warningCount, infoCount, alertTypeBreakdown }

# Workflow
POST /api/inventory-alerts/{alertId}/acknowledge      → status: Acknowledged
POST /api/inventory-alerts/{alertId}/resolve
{ "resolutionNotes": "Reordered 500 units (PO-…)" }
→ status: Resolved, resolvedAt, resolutionNotes saved
```

`alertType` enum (typical): `LowStock`, `OutOfStock`, `ExpiringSoon`, `Expired`. `severity`: `Info`, `Warning`, `Critical`. `status` lifecycle: `Active` → `Acknowledged` → `Resolved`.

---

## E. Reports

| Report | Endpoint | Notable params | Returns |
|--------|----------|---------------|---------|
| Valuation | `/api/inventory-reports/shops/{shopId}/valuation` | — | `totalItems`, `totalUnits`, `totalValue`, items |
| Movement | `/movement` | `fromDate`, `toDate`, `drugId` | adjustment history with before/after qtys |
| ABC analysis | `/abc-analysis` | — | items split A (top 70% value) / B (20%) / C (10%) |
| Expiry | `/expiry` | `daysAhead=90` | counts of expired + 30/60/90-day buckets |
| Turnover | `/turnover` | `fromDate`, `toDate` | `totalSold`, `averageStock`, `turnoverRate`, `daysOfSupply` |
| Dead stock | `/dead-stock` | `daysThreshold=180` | items with no movement for N days |

These are aggregating queries — see best practices below for caching and date-range hygiene.

---

## Best practices

### Security
- **`StockAdjustment` audit rows are the *only* trail of damage/theft/correction events.** Don't expose UPDATE or DELETE on them — they exist for compliance.
- **Stock transfers create a window where stock is "in transit" and not on either shop's shelf.** Make sure both `from` and `to` shops have access controls in your transfer-approval UI; the API doesn't enforce who can approve.
- **Reports may contain PII-adjacent data** (specific batch lots, customer-linked refund adjustments). Restrict access to managers, not cashiers.

### Performance
- **`/inventory-alerts/.../generate` scans every drug in the shop.** Run it on a cron (e.g. nightly), not on every page load. Alerts are persistent — read them with the listing endpoint.
- **Reports `abc-analysis`, `dead-stock`, `turnover` are O(drugs × time-window).** Cache responses for several minutes; restrict date ranges in the UI.
- **Listings of adjustments and transfers grow with shop age.** Always pass a date range; never let staff pull "all time" without warning.

### Correctness
- **Always pass `batchNumber` to `StockAdjustment`.** Without it, the adjustment is ambiguous in mixed-batch inventories — which lot was damaged? Reports can't tell.
- **`StockCount` records the system quantity at *create* time, not at *record* time.** If you schedule a count for tomorrow but stock changes today, the variance reflects the old baseline. Either schedule and immediately count, or use cycle counts on quiet items.
- **`StockTransfer` decrements the sender at *initiation*, not at receipt.** Cancelling before `/receive` reverses; cancelling after `/receive` does not. Keep the workflow strict.
- **Refunds don't auto-create a `Return` adjustment.** When a `SalesOrder` is refunded, file the `StockAdjustment` separately with `adjustmentType: "Return"` and `referenceId: <orderId>`.

### Clean code
- **Use `referenceId` + `referenceType`** to link adjustments to the cause (PO receipt, sales refund, transfer, count variance). Reports become dramatically more useful.
- **Resolve alerts with notes**. The `resolutionNotes` field is what auditors read months later — "Reordered 500 units (PO-2026-0042)" is informative; "fixed" is not.
- **Treat reports as read-only views**. Never mutate state in a reports handler — keep them safe to call repeatedly without side effects.

---

## Next

→ [06 — Cashier / POS Checkout](./06-cashier-pos-checkout.md)
