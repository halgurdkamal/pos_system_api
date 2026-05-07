# 8. Data Model & Cross-API Recipes

The other guides explain each feature in isolation. This one answers the practical questions:

- *Which entity references which?*
- *What's a Category for? A Drug? A ShopInventory? A Batch? When do I touch each one?*
- *How do I get a brand-new product from "nothing exists" to "a customer just bought one"?*

If you just want endpoint reference, skip ahead to the [recipes](#cross-api-recipes).

---

## The cast: every entity in one diagram

```
                                      GLOBAL CATALOG (shared by all shops)
            ┌──────────────────────────────────────────────────────────────────┐
            │                                                                  │
            │   Category                Drug                                   │
            │   ─────────                ─────                                 │
            │   categoryId   ◄──────  categoryId   (Drug stores categoryId    │
            │   name                  drugId        AND a denormalised        │
            │   colorCode             brandName     categoryName snapshot)    │
            │   isActive              genericName                             │
            │                         barcode                                 │
            │                         packagingInfo (Tablet→Strip→Box)        │
            │                         basePricing  (suggested only)           │
            │                         regulatory                              │
            │                                                                 │
            └────────────────────────────────────────┬────────────────────────┘
                                                     │
                                                     │  drugId
                                                     ▼
   PER-SHOP STATE (every shop has its own copy)
   ┌───────────────────────────────────────────────────────────────────────────┐
   │                                                                           │
   │   Shop                ShopUser                ShopInventory               │
   │   ──────              ────────                ────────────                │
   │   shopId    ◄───── shopId               ◄───── shopId                     │
   │   shopName            userId                   drugId        (composite   │
   │   address             role (Cashier…)          shopPricing    key:        │
   │   receiptConfig       permissions[]            reorderPoint   shop+drug)  │
   │   hardwareConfig      isOwner                  totalStock                 │
   │   currency            isActive                 shopFloorStock             │
   │                                                storageStock               │
   │                                                packagingOverride          │
   │                                                batches[] ────────┐        │
   │                                                                  ▼        │
   │                                                         Batch (value obj) │
   │                                                         ───────          │
   │                                                         batchNumber       │
   │                                                         supplierId ───┐  │
   │                                                         qtyOnHand    │  │
   │                                                         expiryDate   │  │
   │                                                         purchasePrice│  │
   │                                                         sellingPrice │  │
   │                                                         status       │  │
   │                                                         location     │  │
   │                                                                      │  │
   └──────────────────────────────────────────────────────────────────────┼──┘
                                                                          │
   COMMERCIAL RECORDS                                                     │
   ┌──────────────────────────────────────────────────────────────────────┼──┐
   │                                                                      ▼  │
   │   Supplier                    PurchaseOrder              SalesOrder     │
   │   ─────────                   ─────────────              ──────────     │
   │   supplierId   ◄──── supplierId            shopId   ──►  shopId         │
   │   name                shopId                cashierId─► (User)          │
   │   contact             status                customerId                  │
   │   paymentTerms        items[] ─► drugId     status                      │
   │                       receipts[] (NOT yet   items[] ─► drugId           │
   │                                  wired to                packagingLevel │
   │                                  inventory)              baseUnitsCons. │
   │                                                          unitPrice      │
   │                                                                          │
   └──────────────────────────────────────────────────────────────────────────┘

   AUDIT / SUPPORTING TABLES
   ┌──────────────────────────────────────────────────────────────────────────┐
   │ StockAdjustment   StockCount    StockTransfer    InventoryAlert          │
   │ (every quantity change has an adjustment row for audit)                  │
   └──────────────────────────────────────────────────────────────────────────┘
```

### What each box is for

| Entity | Purpose | Owned by | Key references |
|--------|---------|----------|----------------|
| **Category** | Group drugs by therapeutic class for browsing/UI colour. | Catalog (global) | — |
| **Drug** | The product definition: brand, generic, barcode, packaging hierarchy, suggested price. **Same record for every shop.** | Catalog (global) | `categoryId` |
| **Shop** | A pharmacy branch. Owns its inventory, prices, sales, members, receipt branding, hardware config. | Tenant root | — |
| **ShopUser** | Joins a `User` to a `Shop` with one role and a permission set. The reason `cashier@A` and `cashier@B` can be the same human. | Shop | `userId`, `shopId` |
| **ShopInventory** | A `(shopId, drugId)` row holding totals, reorder point, shop pricing, packaging overrides, and a list of `Batch` value objects. **Created lazily** the first time you call `POST /api/inventory/shops/{shopId}/stock`. | Shop | `shopId`, `drugId` |
| **Batch** | One physical lot of a drug at a shop: how many units arrived, when, from whom, expiry, purchase price, selling price. Lives **inside** `ShopInventory.batches[]` (not a top-level entity). | ShopInventory | `supplierId` |
| **Supplier** | Who you buy from. Has contact info and default payment terms. | Tenant root (global to your install — not per-shop in current schema) | — |
| **PurchaseOrder** | Buying side: an order to a supplier. Contains lines (`drugId`, qty, unit price). Each line tracks `receipts[]` of partial deliveries. **Does not itself add to ShopInventory** — see [04 — POs](./04-suppliers-and-purchase-orders.md). | Shop | `shopId`, `supplierId`, item.`drugId` |
| **SalesOrder** | Selling side: a customer transaction. Items have `drugId`, `packagingLevel`, `quantity`, `baseUnitsConsumed`. **Does not itself reduce ShopInventory** — see [06 — Cashier](./06-cashier-pos-checkout.md). | Shop | `shopId`, `cashierId`, item.`drugId` |
| **StockAdjustment** | Audit row for every manual quantity change (damage, theft, return, correction). Created automatically by stock-count variance. | Shop | `shopId`, `drugId`, `batchNumber` |
| **StockCount** | A scheduled count event: system qty vs physical qty, with auto-adjustment for variance. | Shop | `shopId`, `drugId` |
| **StockTransfer** | Move stock between shops with approval workflow. | Shop pair | `fromShopId`, `toShopId`, `drugId`, `batchNumber` |
| **InventoryAlert** | Generated low-stock and expiring-batch warnings, with acknowledge/resolve workflow. | Shop | `shopId`, `drugId` |

### How keys flow

- Every per-shop row carries a `shopId` and the auth policy `ShopAccess` checks the JWT against it.
- `Drug.categoryId` → `Category.categoryId`. The drug also stores `categoryName` as a **snapshot** (renaming a category later doesn't propagate).
- `ShopInventory.drugId` → `Drug.drugId`. There is exactly one `ShopInventory` per `(shopId, drugId)` pair.
- `Batch.supplierId` → `Supplier.supplierId`. Set when `AddStock` is called.
- `SalesOrder.items[].drugId` → `Drug.drugId`. The line also stores `packagingLevel` (matched by `unitName`, case-insensitive) and the resulting `baseUnitsConsumed`.
- `PurchaseOrder.supplierId` → `Supplier.supplierId`; `items[].drugId` → `Drug.drugId`.

---

## Cross-API recipes

These recipes call multiple endpoints in order. Each step says **why**.

### Recipe 1 — Onboarding a brand-new pharmacy (the full path)

Use when you've just installed the API and have nothing in the database.

```
Auth
 1. POST /api/auth/register                    → create the founder user
 2. POST /api/auth/login                       → get a JWT
                                                  (handler doesn't grant SuperAdmin —
                                                   keep /register locked down or insert
                                                   the SuperAdmin row directly in DB)

Tenant
 3. POST /api/shops/create-own                 → first shop; user becomes Owner
 4. POST /api/auth/refresh                     → token now carries shop:{shopId}:role=Owner
                                                  (otherwise next call sees no shop claim)
 5. POST /api/shops/{shopId}/members           → invite cashiers / managers (optional)

Catalog (SuperAdmin only — categories & drugs are global)
 6. POST /api/categories                       → at least one category before any drug
 7. POST /api/drugs                            → define the product with packaging
                                                  hierarchy (Tablet → Strip → Box).
                                                  Sets the global default price.

Trade
 8. POST /api/suppliers                        → who you buy from

Stock onboarding (two ways — pick one, see Recipe 2 / 3)
 9. EITHER raise a PO → submit → confirm → receive
    AND ALSO add stock manually (because /receive is a TODO)
 9. OR skip the PO and go straight to POST /api/inventory/shops/{shopId}/stock

Sell
10. POST /api/salesorders → confirm → payment → complete
11. PUT  /api/inventory/shops/{shopId}/drugs/{drugId}/reduce  ← FIFO (manual)
12. GET  /api/pdf/receipt/{orderId}                            ← print
```

> **Step 4 is easy to forget.** A token that was minted before the shop existed has no shop claims. The next call returns 403 even though you "just made the shop." Re-login or call `/refresh`.

### Recipe 2 — Add a brand-new product to one shop's shelves

You want "Amoxil 500" available for a customer to buy at SHOP-A. The catalog already has the drug; you just need to put physical stock on the shelf.

```
Catalog ready?  (SuperAdmin tasks; once-only per drug)
  GET /api/categories?activeOnly=true           → does the right category exist?
  POST /api/categories (if missing)             → create it
  GET /api/drugs/browse?searchTerm=Amoxil       → does the drug exist?
  POST /api/drugs (if missing)                  → create with packaging hierarchy

Shop tasks (Owner / Manager / InventoryClerk)
  POST /api/inventory/shops/SHOP-A/stock        ← creates ShopInventory + first Batch
       {
         "drugId":         "DRG-A1B2C3D4",      ← from catalog
         "supplierId":     "SUP-…",             ← validated to exist
         "batchNumber":    "BATCH-2026-001",
         "quantity":       500,                 ← in base units (tablets)
         "expiryDate":     "2028-05-01",
         "purchasePrice":  5.50,
         "sellingPrice":   12.00,
         "storageLocation":"Shelf A-3"
       }

  PUT  /api/inventory/shops/SHOP-A/drugs/DRG-…/reorder-point
       { "reorderPoint": 50 }                   ← when alerts fire

  PUT  /api/inventory/shops/SHOP-A/drugs/DRG-…/packaging-pricing
       { "PKG-LV-…strip…": 1.50,                ← per-level pricing
         "PKG-LV-…box…":   12.00 }              ← what cashiers actually charge

  POST /api/inventory/shops/SHOP-A/drugs/DRG-…/move-to-floor
       { "quantity": 100, "batchNumber": null } ← null = FEFO (oldest expiry first)
```

After this the drug is sellable: `/pos-items/by-barcode/{barcode}` returns it with stock and price; the cashier can scan it.

> **Where the join happens**: `POST /api/inventory/shops/{shopId}/stock` is the bridge between the catalog (`Drug`) and the shelves (`ShopInventory.batches[]`). The handler validates both `drugId` and `supplierId` exist, then either creates a fresh `ShopInventory` row (with default `reorderPoint=50`, `currency="USD"`, `taxRate=0`) **or** appends a new `Batch` to the existing one.

### Recipe 3 — Receive stock from a purchase order (current reality)

In a fully-wired system, `/api/purchaseorders/{id}/receive` would create the batch on its own. As of today it doesn't (the handler has a `TODO` — see [04](./04-suppliers-and-purchase-orders.md#step-4--receive-stock)). Until that's fixed, do this:

```
1. POST /api/purchaseorders                   → Draft PO with items
2. POST /api/purchaseorders/{id}/submit       → Submitted
3. POST /api/purchaseorders/{id}/confirm      → Confirmed (supplier acknowledged)
4. POST /api/purchaseorders/{id}/receive      → records the receipt on the PO line
                                                 (PO status flips to Completed/Partial)
                                                 — but ShopInventory is NOT changed
5. POST /api/inventory/shops/{shopId}/stock   ← MANUALLY mirror the same batch
                                                 (drugId, supplierId, batchNumber,
                                                  qty, expiry, purchasePrice all
                                                  must match the PO line)
6. POST /api/purchaseorders/{id}/mark-paid    → AP flag, when the invoice is paid
```

When the PO handler is fixed, step 5 disappears.

### Recipe 4 — Daily till workflow (cashier point of view)

```
Login
  POST /api/auth/login

Per customer
  GET  /api/inventory/shops/{shopId}/pos-items/by-barcode/{barcode}
       (or /pos-items?searchTerm=… for type-ahead)
       → drug + availableStock + unitPrice + nearestExpiryDate
                                                 ← scan or search
  POST /api/salesorders                          ← Draft order with items
  POST /api/salesorders/{id}/save-draft          ← park if interrupted
  GET  /api/salesorders/drafts                   ← resume queue
  POST /api/salesorders/{id}/confirm             → Confirmed
  POST /api/salesorders/{id}/payment             → Paid (computes change)
  POST /api/salesorders/{id}/complete            → Completed (timestamp only!)

  ── then for each item on the order ──
  PUT  /api/inventory/shops/{shopId}/drugs/{drugId}/reduce
       { "quantity": <baseUnitsConsumed> }       ← FIFO depletion happens here

  GET  /api/pdf/receipt/{orderId}                ← print

End of day (manager / dashboard)
  GET /api/salesorders/today?shopId=…
  GET /api/salesorders/cashier-performance?shopId=…
  GET /api/salesorders/sales-by-payment-method?shopId=…
  GET /api/salesorders/top-selling-drugs?shopId=…
```

> **The reduce step is the manual band-aid for the inventory-deduction TODO.** When that's fixed, just remove the `PUT /reduce` calls. The cashier UI should always do this loop — without it, stock counts will drift higher than reality and FIFO won't trigger price/batch propagation.

### Recipe 5 — Replenishing the shop floor

The shop floor (`shopFloorStock`) is what the till sees as "available". Storage (`storageStock`) is back-room reserve. Sales reduce `shopFloorStock`; restocking moves between the two.

```
Morning checks
  GET /api/inventory/shops/{shopId}/low-stock
       → items below reorder point
  GET /api/inventory/shops/{shopId}/expiring?days=30
       → batches expiring soon (push these to the floor first)

Move stock onto the floor
  POST /api/inventory/shops/{shopId}/drugs/{drugId}/move-to-floor
       { "quantity": 50, "batchNumber": null }
       ← null batchNumber = FEFO (oldest expiryDate moves first)

If you have too much on the floor (e.g. about to repaint shelves)
  POST /api/inventory/shops/{shopId}/drugs/{drugId}/move-to-storage
       { "quantity": 30, "batchNumber": "BATCH-2026-001" }

End of week — generate alerts
  POST /api/inventory-alerts/shops/{shopId}/generate
  GET  /api/inventory-alerts/shops/{shopId}/summary
  POST /api/inventory-alerts/{alertId}/acknowledge
  POST /api/inventory-alerts/{alertId}/resolve   { "resolutionNotes": "…" }
```

### Recipe 6 — Month-end stock count

Reconcile what the database thinks vs what's on the shelves. Variance auto-creates a `StockAdjustment` row.

```
For each drug in the count
  POST /api/stock-counts/shops/{shopId}
       { "drugId": "DRG-…", "scheduledAt": "2026-05-31T20:00:00Z" }
       → returns systemQuantity (current DB value), status: Scheduled

After physically counting
  POST /api/stock-counts/{countId}/record
       { "physicalQuantity": 485, "varianceReason": "Shrinkage/Loss" }
       → if physical ≠ system, a StockAdjustment is auto-written

  POST /api/stock-counts/{countId}/complete
       → status: Completed

Reports
  GET /api/inventory-reports/shops/{shopId}/valuation
  GET /api/inventory-reports/shops/{shopId}/movement?fromDate=…&toDate=…
  GET /api/inventory-reports/shops/{shopId}/abc-analysis
  GET /api/inventory-reports/shops/{shopId}/dead-stock?daysThreshold=180
```

### Recipe 7 — Move stock from one shop to another

When SHOP-A has surplus and SHOP-B is running low.

```
SHOP-A initiates                       (Owner / Manager of SHOP-A)
  POST /api/stock-transfers/shops/SHOP-A
       { "toShopId": "SHOP-B", "drugId": "DRG-…",
         "batchNumber": "BATCH-…", "quantity": 50 }
       → status: Pending; SHOP-A's stock decremented immediately

Manager approves                       (Owner of either shop, depending on policy)
  POST /api/stock-transfers/{id}/approve
       → status: Approved → InTransit

SHOP-B receives                        (Owner / Manager / Clerk of SHOP-B)
  POST /api/stock-transfers/{id}/receive
       → status: Completed; SHOP-B's stock incremented

If something goes wrong before /receive
  POST /api/stock-transfers/{id}/cancel
       { "reason": "Truck broke down" }
       → status: Cancelled; SHOP-A's reduction is reversed
```

---

## Quick "use which API for what"

| What I want to do | Endpoint family | Guide |
|-------------------|-----------------|-------|
| Sign in, get a token | `/api/auth/*` | [01](./01-authentication.md) |
| Create a pharmacy & invite staff | `/api/shops/*`, `/api/shops/{id}/members` | [02](./02-shops-and-members.md) |
| Add a brand/SKU to the catalogue | `/api/categories`, `/api/drugs` | [03](./03-items-and-catalog.md) |
| Tell the system about a vendor | `/api/suppliers` | [04](./04-suppliers-and-purchase-orders.md) |
| Place an order with that vendor | `/api/purchaseorders/*` | [04](./04-suppliers-and-purchase-orders.md) |
| Put physical stock on a shop's shelves | `POST /api/inventory/shops/{shopId}/stock` | [05](./05-inventory-and-stock.md) |
| Adjust pricing for a specific shop | `PUT /api/inventory/shops/{shopId}/drugs/{drugId}/pricing` (or `/packaging-pricing`) | [05](./05-inventory-and-stock.md) |
| Tell the till about an item / scan a barcode | `GET /api/inventory/shops/{shopId}/pos-items/by-barcode/{barcode}` | [05](./05-inventory-and-stock.md), [06](./06-cashier-pos-checkout.md) |
| Ring up a sale | `/api/salesorders/*` + manual `/api/inventory/.../reduce` | [06](./06-cashier-pos-checkout.md) |
| Print a receipt | `GET /api/pdf/receipt/{orderId}` | [07](./07-barcodes-and-pdf.md) |
| Generate a barcode label | `POST /api/barcodes/generate/barcode` | [07](./07-barcodes-and-pdf.md) |
| Decode a scanned image | `POST /api/barcodes/scan` (then `/search` to resolve) | [07](./07-barcodes-and-pdf.md) |
| Damage write-off / theft / correction | `POST /api/stock-adjustments/shops/{shopId}` | [05](./05-inventory-and-stock.md) |
| Cycle / annual count | `/api/stock-counts/*` | [05](./05-inventory-and-stock.md) |
| Move between shops | `/api/stock-transfers/*` | [05](./05-inventory-and-stock.md) |
| See low / expiring stock | `/api/inventory-alerts/*` + `/api/inventory/shops/{shopId}/low-stock`, `/expiring` | [05](./05-inventory-and-stock.md) |
| Reports (valuation, movement, ABC, dead) | `/api/inventory-reports/*` | [05](./05-inventory-and-stock.md) |
| Sales analytics | `/api/salesorders/dashboard`, `/cashier-performance`, `/top-selling-drugs` | [06](./06-cashier-pos-checkout.md) |
| PO analytics | `/api/purchaseorders/dashboard`, `/supplier-performance`, `/overdue-payments` | [04](./04-suppliers-and-purchase-orders.md) |

---

## What is *not* covered by any endpoint (yet)

These are common pharmacy needs the API doesn't expose today. If a feature you need isn't listed in the recipes above, it might be in this gap list:

- **Customers as a real entity.** `SalesOrder` accepts `customerId`/`customerName`/`customerPhone` strings, but there is no `/api/customers` resource yet. Treat customer info as denormalised on the order.
- **Editing a drug**: `POST /api/drugs` exists; there is no `PUT /api/drugs/{id}` or `DELETE`. To replace a drug you'd need to migrate at the data layer.
- **Drug deactivation per shop**: closest tools are setting `ShopInventory` quantity to 0 or skipping `/move-to-floor`. There's no per-shop "discontinue" flag.
- **Automatic batch creation from PO receive**: see Recipe 3 — the handler is a `TODO`.
- **Automatic stock reduction on sale completion**: see Recipe 4 — the handler is a `TODO`.
- **Refund auto-restoring stock**: `POST /api/salesorders/{id}/refund` flips status only; you must add a `StockAdjustment` of type `Return` to restore the units.
- **Currency at the shop level beyond the `Shop.currency` field**: most price-related responses still hardcode `"USD"` (e.g. `/inventory/shops/{shopId}/value`).

These are the rough edges to plan around when integrating.

---

## Cross-cutting best practices

These apply across the whole API, regardless of which guide you're working from.

### Security
- **The single biggest fix before going to production**: add `[Authorize(Policy = "ShopAccess")]` to the class declaration of `InventoryController`. Today, only the two `pos-items` endpoints check the token; everything else (add stock, reduce stock, change prices) is reachable anonymously. Ditto for the controller-level policy gaps on `/shops/{id}/receipt-config`, `/hardware-config`, and the public `POST /api/barcodes/generate/*`.
- **Run the app over HTTPS only.** JWT bearer tokens are credentials.
- **Treat `/api/auth/register` and `/api/admin/seed` as dev-only routes.** Both are public; `register` doesn't enforce role escalation guards.
- **Do not expose `appsettings.json` JWT secret values via env-var dumps or error pages.** The startup validator rejects placeholders — keep it that way.
- **Audit logs (`StockAdjustment`, PO `receipts[]`, `cancellationReason`, `refund.reason`) are write-once by intent.** Don't expose UPDATE or DELETE on them.
- **PII surfaces**: `customerName`, `customerPhone`, `prescriptionNumber` (in sales orders); supplier `taxId`, `licenseNumber`; user `email`, `phone`. Limit who can list/export, and don't log them at info level.

### Performance
- **Pagination conventions are inconsistent across the API**: most endpoints use `page` + `limit`, but PO and Sales listings use `page` + `pageSize`. Centralise the difference in your client API layer so callers don't have to remember.
- **Catalog reads are public and cheap to cache** (categories rarely change, drugs change occasionally). Stick a CDN or short-TTL HTTP cache in front of `/api/categories` and `/api/drugs/browse`.
- **Per-shop reads (`/inventory`, `/salesorders`, `/purchaseorders`) are dynamic** — no global cache. Prefer client-side caching keyed by `(shopId, last-updated)`.
- **Aggregations** (`/dashboard`, `/supplier-performance`, `/cashier-performance`, `/turnover`) walk full history. Cache responses for 30–60 s; don't poll on every UI refresh.
- **Hot indices** the DB should have for performance-critical paths:
  - `Drug(Barcode)` — every till scan
  - `ShopInventory(ShopId, DrugId)` — every till lookup, every adjustment
  - `SalesOrder(ShopId, OrderDate)` — dashboard, daily reports
  - `Batch.ReceivedDate` — FIFO depletion ordering
  - `Batch.ExpiryDate` — FEFO move-to-floor, expiring-batch report

### Correctness
- **Invariants the API does not enforce — your client must:**
  - Exactly one packaging level should have `isDefault = true`.
  - Exactly one shop member per shop should have `isOwner = true`.
  - One `ShopInventory` per `(shopId, drugId)`. (`AddStock` enforces this; never go around it.)
  - `categoryId` referenced by a drug must exist (handler enforces — but if you delete a category at the DB level, drugs go orphan).
- **Two open TODOs to compensate for in every flow:**
  1. PO `/receive` does not raise `ShopInventory` — mirror with `POST /api/inventory/shops/{shopId}/stock`.
  2. Sales `/complete` does not deduct stock — call `PUT /api/inventory/shops/{shopId}/drugs/{drugId}/reduce` per item afterwards.
  Keep these in a single helper in your client so they're easy to remove when the API is fixed.
- **Race conditions exist on concurrent stock writes.** Use the DB's transactional guarantees; don't rely on application-level checks alone.
- **Time zones**: all server-stored timestamps are UTC. Convert on display, not on storage. Don't round-trip dates through the till as local strings.
- **Decimals**: prices and quantities are `decimal` in C#. If your client uses `double`, you'll accumulate cent-level errors over a busy day. Use a fixed-point or arbitrary-precision type.

### Clean code
- **One client API layer per resource family.** Mirror the guide structure: `auth.ts`, `shops.ts`, `drugs.ts`, `inventory.ts`, `sales.ts`, `pdf.ts`. Don't sprinkle `fetch('/api/...')` calls throughout components.
- **Treat the `Drug` and `Category` as "global config", `Shop`-children as "tenant data".** Different cache strategies, different access controls, different change cadences.
- **Use the slim DTOs (`DrugListItemDto`, `ShopPosItemDto`, `SalesOrderSummaryDto`) for lists.** Reach for the heavy DTOs only when rendering a detail screen.
- **Return early on errors.** Every guide lists the typical 400/404/409 responses — surface those error messages directly to staff (they're written for humans), don't replace with generic "Something went wrong".
- **Idempotency**: most state-changing endpoints are not idempotent. If a network blip leaves you uncertain whether the call succeeded, prefer a follow-up GET to verify state over blind retry.

### Deployment & ops
- **Health checks**: `/health/live` for orchestrators (process up); `/health/ready` for load balancers (DB reachable). Don't point your load balancer at `/health` — it's an alias for live.
- **Logs**: Serilog writes to console and `logs/` (rolling daily, 30-day retention, 10 MB cap). Optionally Seq via `Serilog:Seq:ServerUrl`. See [`../observability.md`](../observability.md).
- **Migrations**: `dotnet ef database update` before each deploy. Catalog tables (Categories, Drugs) and the auth tables are foundational — don't skip migration baselines.
- **Backups**: PostgreSQL native dump on a schedule. The `BackupData` permission exists in the role enum but no endpoint implements it yet — handle backups outside the API.

→ Back to [the index](./README.md).
