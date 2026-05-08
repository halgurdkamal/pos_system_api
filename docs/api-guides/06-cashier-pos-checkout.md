# 6. Cashier / POS Checkout

**What it's for**: the live till. Anything a cashier presses while a customer is at the counter — open an order, scan items, take payment, finalise, print.

**Use this when**: ringing up a sale, parking an order to help another customer, refunding a previous transaction, or pulling up daily totals at the end of a shift.

**Where this fits**: this guide is *commercial*. It writes `SalesOrder` records and runs analytics. As of commit `56d03c0`, `/payment` also debits `ShopInventory` (and `/refund` / `/cancel` restore it) — the till no longer needs a follow-up `/reduce` call.

The till workflow. A `SalesOrder` walks through five states — **Draft → Confirmed → Paid → Completed**, with **Cancelled** and **Refunded** as side branches.

> ✅ **Inventory deduction is automatic on `/payment`** (the historical F-2 gap is closed — see [`99-known-gaps.md#f-2`](./99-known-gaps.md#f-2-sales-complete-does-not-deduct-stock--deduction-now-happens-on-payment-instead)). `/refund` and `/cancel` restore stock symmetrically (F-3). **Do not** keep the legacy `PUT /reduce` workaround in client code — it will double-decrement.

All endpoints require `[Authorize(Policy = "ShopAccess")]`. The cashier's `userId` is taken from the JWT and stamped onto the order — you do **not** pass it in the body.

## Endpoint summary

### The checkout itself

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/salesorders` | Create order with items (Draft) |
| GET  | `/api/salesorders/{id}` | Get full detail |
| GET  | `/api/salesorders` | Paged list with filters |
| POST | `/api/salesorders/{id}/confirm` | Draft → Confirmed |
| POST | `/api/salesorders/{id}/payment` | Confirmed → Paid (**deducts stock**) |
| POST | `/api/salesorders/{id}/complete` | Paid → Completed (handover only — stock already deducted on payment) |
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

> ⚠ This endpoint currently 404s for stocked drugs whose only batches are still in `Storage` (i.e. before any `move-to-floor`). See [`99-known-gaps.md#f-7`](./99-known-gaps.md#f-7-get-apiinventoryshopsshopidpos-itemsby-barcodebarcode-404s-for-stocked-drugs). If your scan flow returns 404 unexpectedly, fall back to `GET /api/barcodes/search?barcode=…` to resolve the drug, then `GET /api/inventory/shops/{shopId}` to read price + stock.

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

Status → **Paid**, `paidAt` stamped. **`ShopInventory` is debited in the same handler** (`SalesStockService.DeductForSaleAsync`) — FIFO across the drug's active batches in the order's shop. If the deduction fails, the order is still marked Paid (no DB transaction wraps both writes) — see the F-2 caveat in [`99-known-gaps.md`](./99-known-gaps.md#f-2-sales-complete-does-not-deduct-stock--deduction-now-happens-on-payment-instead).

### Step 4 — Complete the sale

```http
POST /api/salesorders/SO-uuid/complete
```

What actually happens (from `CompleteSalesOrderCommandHandler`):

1. Loads the order. If status ≠ `Paid`, throws `400` "Can only complete paid orders".
2. Sets status → **Completed** and stamps `completedAt`.
3. Saves and returns the DTO.

That's it. Stock has already been deducted by `/payment` (FIFO across batches by `receivedDate` — see [05 — Inventory: How FIFO works on outflows](./05-inventory-and-stock.md#how-fifo-works-on-outflows)). `/complete` is purely a handover signal: "the goods have left the counter, this transaction is final." Refunds are still allowed from `Completed`; cancels are not.

> ⚠ **Don't add a manual `PUT /reduce` call here** — older versions of this guide instructed clients to do that as a workaround for F-2. F-2 is now closed (deduction happens on `/payment`); a manual reduce will double-decrement and silently corrupt your stock counts.

### Step 5 — Print the receipt

```http
GET /api/pdf/receipt/SO-20260508063819-7292?language=en-US&paperType=Thermal80mm
```

> ⚠ **The path parameter is the order's `orderNumber`, not its `id`.** The route is named `{orderId}` but the handler resolves against `OrderNumber` only — passing the GUID returns 404. See [`99-known-gaps.md#f-6`](./99-known-gaps.md#f-6-get-apipdfreceiptorderid-only-matches-ordernumber-not-the-orders-id).

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
| Inventory | If cancelling a Paid order, stock is restored automatically (handler calls `SalesStockService.RestoreForReversalAsync`); cancelling a Draft/Confirmed order is a no-op for stock since nothing was deducted yet | Stock restored automatically (`SalesStockService.RestoreForReversalAsync`) |
| Money | If Paid, refund issued out-of-band | Refund issued |
| Final status | `Cancelled` | `Refunded` |
| Body | `{ "reason": "…" }` | `{ "reason": "…" }` |

The historical caveat — "refund flips status but doesn't restore stock" — was closed in commit `56d03c0`. No manual `StockAdjustment` follow-up is needed.

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

## Best practices

### Security
- **Every endpoint here is `[Authorize(Policy = "ShopAccess")]`** — keep that. The till handles money; never weaken the policy "for testing".
- **Don't accept full PAN (card numbers) in `paymentReference`.** Use it for last-4 or processor-generated tokens only. Anything stored here ends up in the receipt and order log.
- **Refunds are commercially destructive.** Gate the refund button behind the `RefundSales` permission in the cashier UI even though the API doesn't enforce per-permission checks here today. Audit log refund reasons.
- **Don't log `customerPhone` or `customerName` at info level.** They're PII; downgrade to debug or hash before structured logging.
- **The `cashierId` is taken from the JWT, not from the body.** That's correct — never let the client supply it. (If you add customer-facing self-checkout, build a separate endpoint that doesn't take `shopId` from path either.)

### Performance
- **`/pos-items/by-barcode/{barcode}` is the till's hot path.** Pre-warm on shift start by calling `/pos-items?searchTerm=…&limit=50` for popular categories.
- **The till should keep an in-memory cache of scanned drugs** for the duration of a shift, keyed by barcode. Stock and price update slowly relative to checkout speed; over-fetching kills printer-attached devices.
- **The `dashboard`, `top-selling-drugs`, and `cashier-performance` queries run aggregations.** Refresh on demand (or every 30s), not every keystroke.
- **A long sale with 20+ items means 20+ `PUT /reduce` calls** because of the deduction band-aid. Run them in parallel after `/complete` returns; they touch different `(shopId, drugId)` rows.

### Correctness
- **Always run `PUT /reduce` *after* `/complete` returns 200.** Doing it before risks deducting on a sale that fails finalisation. Doing it inside a transaction with `/complete` would be ideal but isn't possible from a client.
- **Refunds do *not* restore stock.** After `POST /refund`, file a `POST /api/stock-adjustments/shops/{shopId}` with `adjustmentType: "Return"`, the affected `batchNumber`, and a positive `quantityChanged`. Otherwise stock counts drift downward over time.
- **Cancellation rules are enforced server-side**: `Draft` / `Confirmed` / `Paid` are cancellable, `Completed` / `Refunded` are not. Don't show the cancel button after `/complete` succeeded.
- **`packagingLevel` matching is case-insensitive but typos still hurt.** A typo'd level name doesn't 400 — it falls back to "treat quantity as base units", silently under-deducting. Validate against the drug's level list in the till before sending.
- **`amountPaid` must be ≥ `totalAmount`.** The handler rejects insufficient amounts; tip/round-up is allowed but goes into `changeGiven`.
- **One sale = one order**, even with split tender. Today the `payment` endpoint takes a single payment method. If you need split (cash + card), record both in `paymentReference` and pick a primary method.

### Clean code
- **Drive the till from `SalesOrderDto`** as the source of truth. Compute totals server-side; never trust the client's math.
- **`save-draft` is a UI affordance, not a state change.** Treat it as "fetch this order again" — drafts are persisted on creation. This means **the cashier can switch devices mid-sale**: any device with `ShopAccess` can resume a draft.
- **Resume only orders in `Draft` status.** The endpoint enforces this; mirror the check in your UI so users don't try to resume a paid order.
- **Display `orderNumber` (`SO-yyyyMMddHHmmss-xxxx`), not `id`** to staff and customers. The numeric suffix gives lightweight uniqueness; the timestamp is human-friendly for support tickets.
- **Encapsulate the post-`/complete` reduce loop in a single client function** so when the API fixes the auto-deduction TODO, you remove it in one place.

## Next

→ [07 — Barcodes & PDF Receipts](./07-barcodes-and-pdf.md)
