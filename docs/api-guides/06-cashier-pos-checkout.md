# 6. Cashier / POS Checkout

**What it's for**: the live till. Anything a cashier presses while a customer is at the counter — open an order, scan items, take payment, finalise, print.

**Use this when**: ringing up a sale, parking an order to help another customer, refunding a previous transaction, or pulling up daily totals at the end of a shift.

**Where this fits**: this guide is *commercial*. It writes `SalesOrder` records and runs analytics. It does **not** read or write `ShopInventory` directly — the till must call `PUT /api/inventory/.../reduce` after `/complete` to actually deplete stock (see warning below).

The till workflow. A `SalesOrder` walks through five states — **Draft → Confirmed → Paid → Completed**, with **Cancelled** and **Refunded** as side branches.

> ⚠ **Inventory deduction is NOT automatic.** Reading `CreateSalesOrderCommandHandler` and `CompleteSalesOrderCommandHandler` confirms that **none of the sales endpoints currently touch stock** — they don't call `ReduceStockCommand` or `ShopInventory.ReduceStock(...)`. The order is persisted, the status walks through Draft → Confirmed → Paid → Completed, and each line stores `baseUnitsConsumed`, but **batches on `ShopInventory` are never decremented by the sales flow**. To deduct, the till must explicitly call `PUT /api/inventory/shops/{shopId}/drugs/{drugId}/reduce` per item after `/complete` succeeds (see [05 — Inventory](./05-inventory-and-stock.md)). This is an open hole tracked in [`CONTROLLER_REFACTOR_BACKLOG`](../CONTROLLER_REFACTOR_BACKLOG.md).

All endpoints require `[Authorize(Policy = "ShopAccess")]`. The cashier's `userId` is taken from the JWT and stamped onto the order — you do **not** pass it in the body.

## Endpoint summary

### The checkout itself

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/salesorders` | Create order with items (Draft) |
| GET  | `/api/salesorders/{id}` | Get full detail |
| GET  | `/api/salesorders` | Paged list with filters |
| POST | `/api/salesorders/{id}/confirm` | Draft → Confirmed |
| POST | `/api/salesorders/{id}/payment` | Confirmed → Paid |
| POST | `/api/salesorders/{id}/complete` | Paid → Completed (deducts stock) |
| POST | `/api/salesorders/{id}/cancel` | Void with reason |
| POST | `/api/salesorders/{id}/refund` | Reverse a Paid/Completed order |
| POST | `/api/salesorders/{id}/save-draft` | Park current order |
| GET  | `/api/salesorders/{id}/resume` | Resume a parked draft |
| GET  | `/api/salesorders/drafts` | All parked drafts for the cashier |

### Cashier-side analytics & lookups

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/salesorders/dashboard` | Totals, recent orders, top drugs, by-cashier breakdown |
| GET | `/api/salesorders/today` | Today's orders for the shop |
| GET | `/api/salesorders/cashier-performance` | Per-cashier KPIs |
| GET | `/api/salesorders/sales-by-payment-method` | Cash vs card vs … |
| GET | `/api/salesorders/top-selling-drugs?topCount=10` | Best-sellers |
| GET | `/api/inventory/shops/{shopId}/pos-items/by-barcode/{barcode}` | Scan a barcode → drug + price + stock |

## Step-by-step: ring up a sale

### Step 0 — Scan the item (lookup)

Cashier scans a barcode; UI calls:

```http
GET /api/inventory/shops/SHOP-AB12CD34/pos-items/by-barcode/8901234567890
```

Returns a slim DTO with what the till needs:

```json
{
  "drugId": "DRG-A1B2C3D4E5F6",
  "brandName": "Amoxil 500",
  "genericName": "Amoxicillin",
  "availableStock": 490,
  "unitPrice": 12.00,
  "discountPercentage": 0,
  "oldestBatchNumber": "BATCH-2026-001",
  "nearestExpiryDate": "2028-05-01"
}
```

### Step 1 — Open the order (Draft)

```http
POST /api/salesorders

{
  "shopId": "SHOP-AB12CD34",
  "customerId": "CUST-456",
  "customerName": "John Doe",
  "customerPhone": "+1234567890",
  "isPrescriptionRequired": false,
  "prescriptionNumber": null,
  "notes": "Loyalty customer",
  "taxAmount": 5.00,
  "discountAmount": 2.50,
  "items": [
    {
      "drugId": "DRG-A1B2C3D4E5F6",
      "quantity": 2,
      "unitPrice": 25.00,
      "packagingLevel": "Box",
      "discountPercentage": 5.0,
      "batchNumber": "BATCH-2026-001"
    }
  ]
}
```

The handler computes `baseUnitsConsumed` per item (2 boxes × 100 tablets/box = 200 base units), generates an `orderNumber` like `SO-20260508143022-7842`, and stamps `cashierId` from the JWT.

Per-item rules in `CreateSalesOrderCommandHandler`:

- `unitPrice` is **optional**. If omitted, the handler reads `ShopInventory.ShopPricing.GetPackagingLevelPrice(packagingLevel)` for that drug+shop. No price defined ⇒ `400 "No selling price defined for packaging level '…'"`.
- `packagingLevel` is matched **case-insensitively** against the drug's `unitName` ("Box", "BOX", "box" all hit the same level).
- `packagingLevel` is required when `unitPrice` is omitted (it's the lookup key).
- If `packagingLevel` doesn't match any level on the drug, `baseUnitsConsumed` falls back to the literal `quantity` — the line is treated as base units. Pass the right level name to avoid silent under-deduction.
- `discountPercentage` is per-line; the order-level `discountAmount` is applied separately to `subTotal`.

Response:

```json
{
  "id": "SO-uuid",
  "orderNumber": "SO-20260508143022-7842",
  "shopId": "SHOP-AB12CD34",
  "status": "Draft",
  "subTotal": 50.00,
  "taxAmount": 5.00,
  "discountAmount": 2.50,
  "totalAmount": 52.50,
  "amountPaid": 0,
  "changeGiven": 0,
  "paymentMethod": null,
  "cashierId": "USER-9F3A2C1B",
  "items": [
    {
      "id": "SOI-…",
      "drugId": "DRG-A1B2C3D4E5F6",
      "quantity": 2,
      "unitPrice": 25.00,
      "discountPercentage": 5.0,
      "discountAmount": 2.50,
      "totalPrice": 47.50,
      "packagingLevelSold": "Box",
      "batchNumber": "BATCH-2026-001",
      "baseUnitsConsumed": 200.0
    }
  ]
}
```

### Step 1b (optional) — Park / resume

A cashier juggling multiple customers can park the current order and start a new one:

```http
POST /api/salesorders/SO-uuid/save-draft   → returns the order (it's already persisted as Draft on create)
GET  /api/salesorders/drafts               → list all parked drafts (auto-filtered to caller's cashierId)
GET  /api/salesorders/SO-uuid/resume       → re-fetch; rejected with 400 if status ≠ "Draft"
```

Implementation note: the order is already a `Draft` from the moment `POST /api/salesorders` returns. `save-draft` doesn't change anything in the DB — it's a UI-affordance that re-fetches the record so the front-end can clear the till and let the cashier start another order.

### Step 2 — Confirm

```http
POST /api/salesorders/SO-uuid/confirm
```

Validates the order has at least one item; transitions Draft → **Confirmed**. After confirm, items can no longer be added/removed without going back to Draft.

### Step 3 — Take payment

```http
POST /api/salesorders/SO-uuid/payment

{
  "paymentMethod": "Cash",
  "amountPaid": 60.00,
  "paymentReference": null
}
```

`paymentMethod` examples: `Cash`, `CreditCard`, `DebitCard`, `MobileMoney`, `BankTransfer`. Use `paymentReference` for the card auth code or transfer ID.

Validation:
- `amountPaid >= totalAmount`
- Computes `changeGiven = amountPaid - totalAmount`

Status → **Paid**, `paidAt` stamped.

### Step 4 — Complete the sale

```http
POST /api/salesorders/SO-uuid/complete
```

What actually happens (from `CompleteSalesOrderCommandHandler`):

1. Loads the order. If status ≠ `Paid`, throws `400` "Can only complete paid orders".
2. Sets status → **Completed** and stamps `completedAt`.
3. Saves and returns the DTO.

That's it. **No inventory call**, no FIFO deduction, no propagation to analytics. The till must call `PUT /api/inventory/shops/{shopId}/drugs/{drugId}/reduce` for each item after this returns OK — using each line's `baseUnitsConsumed`:

```http
# For every item in the completed order:
PUT /api/inventory/shops/SHOP-AB12CD34/drugs/DRG-A1B2C3D4/reduce
{ "quantity": 200 }   # = baseUnitsConsumed from the SalesOrderItem
```

That call walks `ShopInventory.ReduceStock`, which sorts active batches by `receivedDate` ascending and depletes them in order — see [05 — Inventory: How FIFO works on outflows](./05-inventory-and-stock.md#how-fifo-works-on-outflows).

If you keep the till "minimal", you can chain confirm + payment + complete + N reduce-stock calls after a single button press.

### Step 5 — Print the receipt

```http
GET /api/pdf/receipt/SO-uuid?language=en-US&paperType=Thermal80mm
```

Returns a PDF binary stream. Receipt branding (logo, footer, VAT line) comes from the shop's `receiptConfig` (see [02 — Shops](./02-shops-and-members.md)). Details in [07 — PDF](./07-barcodes-and-pdf.md).

## Status lifecycle

```
              ┌───────────────────────────────── cancel ────► Cancelled
              │ (allowed from Draft / Confirmed / Paid;
              │  not after Completed)
              ▼
   Draft ── confirm ──► Confirmed ── /payment ──► Paid ── /complete ──► Completed
                                                           │                  │
                                                           └─── /refund ──────┴──► Refunded
```

Terminal states: **Completed**, **Cancelled**, **Refunded**.

## Cancel vs Refund

| | Cancel | Refund |
|---|--------|--------|
| Allowed from | Draft / Confirmed / Paid | Paid / Completed |
| Inventory | Not deducted yet (or rolled back if Paid pre-complete) | Already deducted — manual restoration via `StockAdjustment` of type `Return` |
| Money | If Paid, refund issued out-of-band | Refund issued |
| Final status | `Cancelled` | `Refunded` |
| Body | `{ "reason": "…" }` | `{ "reason": "…" }` |

> **Heads-up**: the `refund` endpoint flips status but does **not** automatically restore stock. The cashier (or a manager) must follow up with `POST /api/stock-adjustments/shops/{shopId}` `adjustmentType: "Return"` on the affected batch. Track the `referenceId` field of the adjustment with the order ID so the audit trail links them.

## Filtering & analytics

`GET /api/salesorders` accepts: `shopId`, `cashierId`, `customerId`, `status`, `fromDate`, `toDate`, `paymentMethod`, `searchTerm`, `page`, `pageSize` (note: `pageSize`, not `limit`).

Quick KPIs:

```http
GET /api/salesorders/dashboard?shopId=SHOP-AB12CD34&fromDate=2026-05-01&toDate=2026-05-08
```

Returns a `SalesOrderDashboardDto` with totals, counts, recent orders, and breakdowns. Adjacent endpoints:

- `GET /sales-by-payment-method?shopId=…&fromDate=…&toDate=…` → `Dictionary<string, decimal>` keyed by payment method (e.g. `{ "Cash": 1240.50, "CreditCard": 380.00 }`).
- `GET /top-selling-drugs?shopId=…&topCount=10&fromDate=…&toDate=…` → `Dictionary<string, int>` keyed by `drugId` with units sold as the value.
- `GET /cashier-performance?shopId=…` → list of `CashierPerformanceDto` per cashier (orders, sales, average, today's totals).

## Common pitfalls

- **Selling more than stock** — the create command does not block over-sell at Draft time; it only fails at `/complete` when FIFO runs out. Do a `pos-items/by-barcode` lookup before adding.
- **Wrong packaging level** — `packagingLevel` must match a level in the drug's hierarchy and that level must be `isSellable=true` (or a per-shop override that allows it).
- **Skipping confirm** — payment and complete will both reject a Draft order.
- **Using a refund as a void** — refund implies money goes back. Use `cancel` for orders that never collected payment.

## Next

→ [07 — Barcodes & PDF Receipts](./07-barcodes-and-pdf.md)
