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
- **Term you don't recognise?** Check [`00-glossary.md`](./00-glossary.md) — FIFO, FEFO, SKU, PII, etc.
- **Bug or surprising behaviour?** Check [`99-known-gaps.md`](./99-known-gaps.md) — the canonical list of issues and workarounds.

## The guides

Read in order if you're new — each one builds on the last.

| # | Guide | What you can do after reading |
|---|-------|------------------------------|
| 0 | [Glossary](./00-glossary.md) | One-stop reference for FIFO, FEFO, SKU, EAN, PII, CQRS, and other terms used in the guides |
| 1 | [Authentication](./01-authentication.md) | Register, log in, refresh tokens, understand roles & permissions |
| 2 | [Shops & Members](./02-shops-and-members.md) | Create a shop, add cashiers/managers, configure receipts |
| 3 | [Items & Catalog (Drugs)](./03-items-and-catalog.md) | Create categories, add a drug with packaging hierarchy, search the catalog |
| 4 | [Suppliers & Purchase Orders](./04-suppliers-and-purchase-orders.md) | Add a supplier, raise a PO, receive stock |
| 5 | [Inventory & Stock — index](./05-inventory-and-stock.md) | Splits into 5a (core) and 5b (operations) |
| 5a | [Inventory Core](./05a-inventory-core.md) | View stock, add stock, pricing, packaging overrides, FIFO mechanics |
| 5b | [Stock Operations](./05b-stock-operations.md) | Adjustments, counts, transfers, alerts, reports |
| 6 | [Cashier / POS Checkout](./06-cashier-pos-checkout.md) | Run the till: start an order, scan items, take payment, complete |
| 7 | [Barcodes & PDF Receipts](./07-barcodes-and-pdf.md) | Generate barcodes/QR codes, scan them, print receipts |
| 8 | [Data Model & Recipes](./08-data-model-and-recipes.md) | How the entities join up, plus end-to-end cookbook recipes spanning multiple APIs |
| 99 | [Known Gaps](./99-known-gaps.md) | Canonical list of security gaps, functional TODOs, and workarounds |

If you're new and want one place that explains how everything fits together, **start with [08 — Data Model & Recipes](./08-data-model-and-recipes.md)**, then dip into 1–7 for the deep dives.

## End-to-end story

A full real-world cycle calls roughly this sequence:

```
1. POST /api/auth/login                              → token
2. POST /api/shops/create-own                        → shop (re-login to pick up the new shop claim)
3. POST /api/shops/{shopId}/members                  → add cashier
4. POST /api/categories                              → category
5. POST /api/drugs                                   → drug + packaging
6. POST /api/suppliers                               → supplier
7. POST /api/purchaseorders                          → PO (Draft)
   → /submit → /confirm → /receive                   → PO line records receipt + appends Batch to ShopInventory
                                                       (first-receipt-of-a-drug bug — see 99-known-gaps F-1)
8. POST /api/salesorders                             → cashier opens order
   → add items → /confirm → /payment                 → stock auto-deducted FIFO (was F-2 — now closed)
                          → /complete                → handover; no further inventory call needed
9. GET  /api/pdf/receipt/{orderNumber}               → PDF receipt (key by orderNumber, not id — F-6)
```

Each guide expands one of those rows. **All `DateTime` fields in the JSON bodies must be ISO-8601 with explicit UTC** (`"…T00:00:00Z"`) until F-5 is fixed — bare date literals trigger a Postgres `Kind=Unspecified` error.
