# API Guides — How Each Feature Works

Step-by-step walkthroughs of every major feature in the POS System API, with the exact endpoints, request/response payloads, status lifecycles, and the order to call them in.

These guides are **task-oriented** ("I want to ring up a sale", "I want to receive a purchase order"). For domain-model deep-dives (entities, packaging math, batch propagation), see the topic folders next to this one (`../drugs/`, `../packaging/`, `../pricing/`).

## Common conventions

- **Base URL**: `http://localhost:5000` in dev. All routes start with `/api/`.
- **Auth header** (every protected endpoint): `Authorization: Bearer <accessToken>`
- **Content type**: `application/json` unless noted (barcode scan upload is `multipart/form-data`).
- **Multi-tenant**: most endpoints take `shopId` in the route. The user's JWT must contain a `shop:{shopId}:role` claim or be `SuperAdmin`.
- **IDs**: prefixed strings — `USER-…`, `SHOP-…`, `DRG-…`, `CAT-…`, `SUP-…`, `PO-…`, `INV-…`. Generated server-side unless you supply one.
- **Swagger**: live spec at <http://localhost:5000/swagger>.

## The guides

Read in order if you're new — each one builds on the last.

| # | Guide | What you can do after reading |
|---|-------|------------------------------|
| 1 | [Authentication](./01-authentication.md) | Register, log in, refresh tokens, understand roles & permissions |
| 2 | [Shops & Members](./02-shops-and-members.md) | Create a shop, add cashiers/managers, configure receipts |
| 3 | [Items & Catalog (Drugs)](./03-items-and-catalog.md) | Create categories, add a drug with packaging hierarchy, search the catalog |
| 4 | [Suppliers & Purchase Orders](./04-suppliers-and-purchase-orders.md) | Add a supplier, raise a PO, receive stock — which creates batches |
| 5 | [Inventory & Stock Operations](./05-inventory-and-stock.md) | View stock, adjust, count, transfer between shops, react to alerts |
| 6 | [Cashier / POS Checkout](./06-cashier-pos-checkout.md) | Run the till: start an order, scan items, take payment, complete & deduct stock |
| 7 | [Barcodes & PDF Receipts](./07-barcodes-and-pdf.md) | Generate barcodes/QR codes, scan them, print receipts |
| 8 | [Data Model & Recipes](./08-data-model-and-recipes.md) | How the entities join up, plus end-to-end cookbook recipes spanning multiple APIs |

If you're new and want one place that explains how everything fits together, **start with [08 — Data Model & Recipes](./08-data-model-and-recipes.md)**, then dip into 1–7 for the deep dives.

## End-to-end story

A full real-world cycle calls roughly this sequence:

```
1. POST /api/auth/login                              → token
2. POST /api/shops/create-own                        → shop
3. POST /api/shops/{shopId}/members                  → add cashier
4. POST /api/categories                              → category
5. POST /api/drugs                                   → drug + packaging
6. POST /api/suppliers                               → supplier
7. POST /api/purchaseorders                          → PO (Draft)
   → /submit → /confirm → /receive                   → batches created, stock available
8. POST /api/salesorders                             → cashier opens order
   → add items → /confirm → /payment → /complete     → stock deducted (FIFO)
9. GET  /api/pdf/receipt/{orderId}                   → PDF receipt
```

Each guide expands one of those rows.
