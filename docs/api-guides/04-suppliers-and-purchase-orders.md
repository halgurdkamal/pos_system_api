# 4. Suppliers & Purchase Orders

**What it's for**: tracking the buying side — who you order from and what's on order. POs give you a paper trail (status, expected delivery, invoice payment) plus supplier-performance analytics.

**Use this when**: you want a record of an order placed with a vendor; you need supplier KPIs (on-time rate, fill rate); you need to flag overdue payments. The PO is the primary commercial record between you and a supplier.

> ⚠ **Do NOT rely on the PO `/receive` endpoint to add stock to your shelves** — its inventory step is currently a `TODO` (see Step 4 below). Stock onboarding goes through `POST /api/inventory/shops/{shopId}/stock` instead. See [Recipe 3 in the cookbook](./08-data-model-and-recipes.md#recipe-3--receive-stock-from-a-purchase-order-current-reality).

A purchase order (PO) is the path stock *should* take from a **supplier** into a **shop's inventory**. Each receipt event records a `Receipt` on the PO line; the matching batch must currently be added on `ShopInventory` separately.

## Endpoint summary

### Suppliers

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST   | `/api/suppliers` | bearer | Create |
| GET    | `/api/suppliers?page=&limit=&isActive=&supplierType=` | bearer | Paged list |
| GET    | `/api/suppliers/active` | bearer | Active only |
| GET    | `/api/suppliers/{id}` | bearer | One |
| PUT    | `/api/suppliers/{id}` | bearer | Update |
| DELETE | `/api/suppliers/{id}` | bearer | Soft delete (`isActive=false`) |

### Purchase orders (all `ShopAccess`)

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/purchaseorders` | Create (Draft) |
| GET  | `/api/purchaseorders?shopId=&supplierId=&status=&fromDate=&toDate=&priority=&isPaid=&searchTerm=&page=&pageSize=` | Paged list (note: `pageSize`, not `limit`) |
| GET  | `/api/purchaseorders/{id}` | One |
| POST | `/api/purchaseorders/{id}/submit` | Draft → Submitted |
| POST | `/api/purchaseorders/{id}/confirm` | Submitted → Confirmed |
| POST | `/api/purchaseorders/{id}/receive` | Receive stock (creates batches) |
| POST | `/api/purchaseorders/{id}/cancel` | Cancel with reason |
| POST | `/api/purchaseorders/{id}/mark-paid` | Flip `isPaid=true` |
| GET  | `/api/purchaseorders/dashboard?shopId=` | Aggregate KPIs |
| GET  | `/api/purchaseorders/pending?shopId=` | Open POs |
| GET  | `/api/purchaseorders/overdue-payments?shopId=` | Past due |
| GET  | `/api/purchaseorders/supplier-performance?shopId=` | On-time / fill-rate metrics |

## Step-by-step: a PO from raise to receive

### Step 1 — Create the supplier (once)

```http
POST /api/suppliers

{
  "supplierName": "Alpha Pharmaceuticals",
  "supplierType": "Distributor",
  "contactNumber": "+9647901234567",
  "email": "sales@alpha.com",
  "address": {
    "street": "123 Medical St",
    "city": "Baghdad",
    "country": "Iraq"
  },
  "paymentTerms": "Net 30",
  "deliveryLeadTime": 7,
  "minimumOrderValue": 500000,
  "taxId": "IRQ123456789",
  "licenseNumber": "DIST-9981"
}
```

`supplierType` enum: `Manufacturer`, `Distributor`, `Wholesaler`, `LocalAgent`.

Response → `SupplierDto` with `id` like `SUP-456def`.

### Step 2 — Raise a draft PO

```http
POST /api/purchaseorders

{
  "shopId": "SHOP-AB12CD34",
  "supplierId": "SUP-456def",
  "priority": "High",
  "expectedDeliveryDate": "2026-05-15",
  "paymentTerms": "Net30",
  "customPaymentTerms": null,
  "deliveryAddress": "Main warehouse, gate 2",
  "referenceNumber": "REQ-2026-0042",
  "notes": "Urgent restock for Eid season",
  "shippingCost": 50000,
  "taxAmount": 25000,
  "discountAmount": 50000,
  "items": [
    {
      "drugId": "DRG-A1B2C3D4E5F6",
      "quantity": 100,
      "unitPrice": 5000,
      "discountPercentage": 10
    }
  ]
}
```

Enums:
- `priority` — `Low`, `Normal`, `High`, `Urgent`
- `paymentTerms` — `Immediate`, `Net15`, `Net30`, `Net45`, `Net60`, `Custom` (then fill `customPaymentTerms`)

Response (`PurchaseOrderDto`) — note the auto-generated `orderNumber` and `Draft` status:

```json
{
  "id": "PO-9c8b7a6f",
  "orderNumber": "PO-20260508-5798",
  "shopId": "SHOP-AB12CD34",
  "supplierId": "SUP-456def",
  "status": "Draft",
  "priority": "High",
  "subTotal": 500000,
  "taxAmount": 25000,
  "shippingCost": 50000,
  "discountAmount": 50000,
  "totalAmount": 525000,
  "paymentTerms": "Net30",
  "paymentDueDate": "2026-06-07",
  "isPaid": false,
  "createdBy": "USER-9F3A2C1B",
  "items": [
    {
      "id": "POITEM-xyz",
      "drugId": "DRG-A1B2C3D4E5F6",
      "orderedQuantity": 100,
      "unitPrice": 5000,
      "discountPercentage": 10,
      "totalPrice": 450000,
      "receivedQuantity": 0,
      "pendingQuantity": 100,
      "isFullyReceived": false,
      "receipts": []
    }
  ],
  "completionPercentage": 0
}
```

### Step 3 — Submit & confirm

```http
POST /api/purchaseorders/PO-9c8b7a6f/submit
```

Sets `Status=Submitted`, stamps `submittedAt`/`submittedBy`. This is the "sent to supplier" event — typically you'd email the PDF here.

```http
POST /api/purchaseorders/PO-9c8b7a6f/confirm
```

Sets `Status=Confirmed`. Use when the supplier acknowledges the order.

### Step 4 — Receive stock

You can call this multiple times for partial deliveries.

```http
POST /api/purchaseorders/PO-9c8b7a6f/receive

{
  "items": [
    {
      "itemId": "POITEM-xyz",
      "quantity": 50,
      "batchNumber": "BATCH-2026-001",
      "expiryDate": "2028-05-01"
    }
  ]
}
```

What happens server-side:

1. Handler loads the PO and the matching `PurchaseOrderItem` (by `itemId`). Mismatched item ids are silently skipped (logged as a warning).
2. Calls `orderItem.ReceiveQuantity(50, "BATCH-2026-001", expiry, receivedBy)` which appends a `Receipt` record to the item's history (`receipts[]`) and bumps `receivedQuantity`.
3. PO status updates:
   - `PartiallyReceived` once any item has `receivedQuantity > 0`.
   - `Completed` when **every** item satisfies `IsFullyReceived()`. `completedAt` is stamped.

> ⚠ **Reality check**: as of this writing, the handler's `UpdateInventoryAsync` method **does not actually create `Batch` records on `ShopInventory`** — it only logs the receipt and carries a `TODO` comment ("Integrate with StockAdjustment system to properly track batches, expiry dates, and update inventory totals"). So:
>
> - **The PO is the source of truth for *what was received*.**
> - **Stock on `ShopInventory` is not raised by the receive endpoint.** Until that handler is finished, you must add the batch separately via `POST /api/inventory/shops/{shopId}/stock` (covered in [05 — Inventory](./05-inventory-and-stock.md)) — typically right after `/receive` succeeds.
>
> Keep this in mind when building UIs: don't trust the till to see new stock just because a PO was marked `Completed`. Tracked in [`CONTROLLER_REFACTOR_BACKLOG`](../CONTROLLER_REFACTOR_BACKLOG.md).

For how prices propagate from a newly-active batch into the shop's selling price, see [`../pricing/from-batch-guide.md`](../pricing/from-batch-guide.md).

### Step 5 — Pay it

```http
POST /api/purchaseorders/PO-9c8b7a6f/mark-paid

# Optional body — backdate the payment:
{ "paidAt": "2026-05-07T14:30:00Z" }
```

Sets `isPaid=true`. `paidAt` defaults to "now" if you omit the body or don't supply `paidAt`. This endpoint doesn't move money — it's a record-keeping flag for AP reporting.

### Cancel path

```http
POST /api/purchaseorders/PO-9c8b7a6f/cancel
{ "reason": "Supplier discontinued the SKU" }
```

Allowed from `Draft`, `Submitted`, `Confirmed`. Disallowed once any stock has been received.

## Status lifecycle

```
Draft ── submit ──► Submitted ── confirm ──► Confirmed ── receive (partial) ──► PartiallyReceived
   │                   │                          │                                    │
   │                   │                          └──── receive (all) ─────────────────┴──► Completed
   │                   │                                                                       │
   └─── cancel ────────┴── cancel ──┐                                                          │
                                    ▼                                                          │
                              Cancelled  ◄─── (cannot cancel after first receipt) ─────────────┘
```

`Completed` and `Cancelled` are terminal. Payment is an orthogonal flag (`isPaid`) — a Completed PO can still be unpaid.

## Dashboard endpoints — what they return

| Endpoint | Useful for |
|----------|-----------|
| `/dashboard?shopId=…` | Counts per status, total open value, this-week receipts, top suppliers |
| `/pending?shopId=…` | Anything not yet `Completed`/`Cancelled` — buyer's worklist |
| `/overdue-payments?shopId=…` | `paymentDueDate < today AND isPaid=false` |
| `/supplier-performance?shopId=…` | On-time delivery %, fill rate, avg lead time per supplier |

## Common pitfalls

- **Receiving more than ordered** — handler rejects; raise a separate adjustment via `/api/stock-adjustments` instead.
- **Forgetting `expiryDate`** — required for pharma batches; receipt fails validation without it.
- **Cancelling after partial receipt** — disallowed. Use `StockAdjustment` of type `Return` to remove the bad stock instead.

## Best practices

### Security
- **All PO endpoints carry `[Authorize(Policy = "ShopAccess")]`** — keep that. POs contain commercial data (unit prices, supplier IDs) that competitors would value.
- **Don't expose supplier endpoints to non-staff.** Suppliers carry payment terms and license numbers. The current policy is `[Authorize]` (any authenticated user) — tighten to `ShopOwnerOrAdmin` if non-staff users (e.g. customers) ever get accounts.
- **`DELETE /api/suppliers/{id}` is a soft delete** (`isActive=false`). Good — historical POs need a stable supplier reference.
- **Supplier banking details (if you store them via `taxId` / `licenseNumber`)** should not appear in PO listings or PDFs that go outside the org.

### Performance
- **`/api/purchaseorders/dashboard` and `/supplier-performance` aggregate across the shop's full PO history.** Cache the response for 30–60 seconds in the client; pharmacy KPIs don't need real-time refresh.
- **List query is paginated** with `page` + `pageSize`. Default 20 is fine for an inbox view; for a CSV export use a higher page size (≤ 200) and walk pages.
- **Receive in batches.** A delivery of 50 SKUs is one `POST /receive` call with 50 items — don't do 50 round-trips. The handler iterates internally.
- **`searchTerm` does substring matching.** Index-friendly only if your DB has trigram or similar; for shops with > 10k POs, narrow the date range first.

### Correctness
- **Mirror every PO receipt with `POST /api/inventory/shops/{shopId}/stock`** until the `/receive` handler is fixed. Use the *same* `batchNumber`, `quantity`, `expiryDate`, `purchasePrice`, and `supplierId` so the audit trail lines up.
- **`/receive` partial deliveries are idempotent on receipt rows but additive on quantities.** Calling it twice with the same body adds quantity twice; sending an unknown `itemId` is silently ignored. Validate before posting.
- **You cannot cancel a PO once any line has `receivedQuantity > 0`.** Use a `StockAdjustment` of type `Return` against the affected batch to remove the bad stock instead.
- **Don't rely on `paymentDueDate` for cash flow alarms.** It's derived from `paymentTerms` at create-time; manually pushing `mark-paid` doesn't recompute future due dates on related POs.
- **Item-line discount vs order-level discount differ in math.** `discountPercentage` on an item reduces `totalPrice`; order-level `discountAmount` reduces `subTotal`. Pick one model per shop and stick with it.

### Clean code
- **Use `referenceNumber` for your internal requisition number.** It's a free-text field designed for cross-system tracing — invoice numbers, internal procurement ticket IDs, etc.
- **Submit → confirm → receive should be three separate user actions in your UI**, even if they happen seconds apart. Each transitions a state machine — collapsing them hides errors (e.g. supplier rejecting an order).
- **Treat `mark-paid` as an accounting concern, not a workflow step.** Wire it from the AP module, not the warehouse UI.

## Next

→ [05 — Inventory & Stock Operations](./05-inventory-and-stock.md)
