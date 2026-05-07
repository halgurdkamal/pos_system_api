# 3. Items & Catalog (Drugs)

**What it's for**: defining what *exists in the world* — the SKU. Every brand/strength/form of medicine your business sells is one `Drug` record, shared by every shop. Categories group drugs for browsing.

**Use this when**: you're stocking a new product for the first time anywhere. **Do this before** anyone tries to add stock or sell it — `POST /api/inventory/.../stock` and `POST /api/salesorders` both look up `drugId` and reject unknown ones.

**Do NOT use this for** per-shop pricing or stock. Those live on `ShopInventory` (see [05](./05-inventory-and-stock.md)). The `basePricing.suggestedRetailPrice` here is just a hint — the till uses the per-shop number.

The drug catalog is **global and shared** across shops — every shop sells the same `Drug` records, but each shop owns its own pricing, packaging overrides, and stock. A drug has:

- a **category** (used for grouping & UI colour-coding)
- a **packaging hierarchy** (e.g. base unit *Tablet* → *Strip of 10* → *Box of 100*)
- regulatory & formulation metadata
- one or more shop-specific `ShopInventory` records (covered in [05 — Inventory](./05-inventory-and-stock.md))

For the deeper model (entities, relationships, lifecycle), see [`../drugs/drug-system.md`](../drugs/drug-system.md). This guide focuses on **how to call the API**.

## Endpoint summary

### Categories

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/categories?activeOnly=true` | public | List |
| POST | `/api/categories` | `AdminOnly` | Create |

### Drugs

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET | `/api/drugs?page=1&limit=20` | public | Paged list (raw) |
| GET | `/api/drugs/{id}` | public | Single drug |
| GET | `/api/drugs/browse?searchTerm=&category=&inStock=` | public | Search/filter — main listing endpoint |
| GET | `/api/drugs/{id}/detail` | public | Drug + per-shop inventory summary |
| POST | `/api/drugs` | `AdminOnly` | Create |

> **Browse vs list**: `/browse` returns a slim `DrugListItemDto` shaped for storefront rendering (with category colour, primary image, prescription flag). `/` returns the full `DrugDto` with packaging.

## Step-by-step: add a new drug to the catalog

### Step 1 — Make sure the category exists

```http
POST /api/categories
Authorization: Bearer …(SuperAdmin)…

{
  "name": "Antibiotics",
  "logoUrl": "https://cdn.pharma.com/cat/antibiotics.png",
  "description": "Antibiotic medications",
  "colorCode": "#FF5733",
  "displayOrder": 1
}
```

Response `201`:

```json
{
  "categoryId": "CAT-A1B2C3D4",
  "name": "Antibiotics",
  "isActive": true
}
```

### Step 2 — Create the drug

The full payload is large. **Start with this minimal version** to confirm the call works, then enrich.

#### Minimal drug (just the required fields)

```http
POST /api/drugs
Authorization: Bearer …(SuperAdmin)…

{
  "brandName":   "Amoxil 500",
  "genericName": "Amoxicillin",
  "categoryId":  "CAT-A1B2C3D4",
  "packagingInfo": {
    "unitType":            "Count",
    "baseUnit":            "tablet",
    "baseUnitDisplayName": "Tablet",
    "packagingLevels": [
      {
        "levelNumber":      1,
        "unitName":         "Tablet",
        "baseUnitQuantity": 1,
        "isSellable":       true,
        "isDefault":        true
      }
    ]
  }
}
```

That's the smallest valid `POST /api/drugs` body — four scalar fields plus one packaging level. The handler will fill in defaults: `barcodeType = "EAN-13"`, `imageUrls = []`, etc., and auto-generate the `drugId`.

Once the minimal call works, expand to the **full payload** below to add barcode, regulatory info, multi-level packaging, base pricing hint, and metadata.

#### Full drug payload

Build it in three logical pieces: identity, regulatory/formulation, and the **packaging hierarchy**.

```http
POST /api/drugs
Authorization: Bearer …(SuperAdmin)…

{
  "brandName": "Amoxil 500",
  "genericName": "Amoxicillin",
  "manufacturer": "GSK",
  "originCountry": "UK",
  "categoryId": "CAT-A1B2C3D4",
  "barcode": "8901234567890",
  "barcodeType": "EAN-13",
  "imageUrls": ["https://cdn.pharma.com/drug/amoxil.jpg"],
  "description": "Broad-spectrum antibiotic (penicillin family)",
  "sideEffects": ["Nausea", "Rash"],
  "interactionNotes": ["Reduces effectiveness of oral contraceptives"],
  "tags": ["oral", "antibiotic", "penicillin"],
  "relatedDrugs": [],

  "formulation": {
    "form": "Capsule",
    "strength": "500mg",
    "routeOfAdministration": "Oral"
  },

  "basePricing": {
    "suggestedRetailPrice": 5.99,
    "currency": "USD",
    "suggestedTaxRate": 0.10
  },

  "regulatory": {
    "isPrescriptionRequired": true,
    "isHighRisk": false,
    "drugAuthorityNumber": "FDA-123456",
    "approvalDate": "2020-01-15",
    "controlSchedule": "Schedule IV"
  },

  "packagingInfo": {
    "unitType": "Count",
    "baseUnit": "tablet",
    "baseUnitDisplayName": "Tablet",
    "isSubdivisible": true,
    "packagingLevels": [
      {
        "levelNumber": 1,
        "unitName": "Tablet",
        "baseUnitQuantity": 1,
        "isSellable": false,
        "isDefault": false,
        "isBreakable": true
      },
      {
        "levelNumber": 2,
        "unitName": "Strip",
        "baseUnitQuantity": 10,
        "quantityPerParent": 10,
        "isSellable": true,
        "isDefault": false,
        "isBreakable": true,
        "barcode": "8901234567891"
      },
      {
        "levelNumber": 3,
        "unitName": "Box",
        "baseUnitQuantity": 100,
        "quantityPerParent": 10,
        "isSellable": true,
        "isDefault": true,
        "isBreakable": false,
        "barcode": "8901234567890",
        "minimumSaleQuantity": 1
      }
    ]
  }
}
```

Response `201`:

```json
{
  "drugId": "DRG-A1B2C3D4",
  "barcode": "8901234567890",
  "brandName": "Amoxil 500",
  "category": "Antibiotics",
  "packagingInfo": {
    "packagingLevels": [
      { "packagingLevelId": "PKG-LV-…", "levelNumber": 1, "unitName": "Tablet" },
      { "packagingLevelId": "PKG-LV-…", "levelNumber": 2, "unitName": "Strip" },
      { "packagingLevelId": "PKG-LV-…", "levelNumber": 3, "unitName": "Box" }
    ]
  },
  "createdAt": "2026-05-08T10:00:00Z"
}
```

What the handler enforces (from `CreateDrugCommandHandler`):
- `brandName`, `genericName`, `categoryId`, and `packagingInfo.packagingLevels` (at least one) are **required** (FluentValidation, returns 400).
- `categoryId` must resolve to a real category — else `404 NotFound`.
- If you pass `drugId` and one already exists ⇒ `409 Conflict`.
- If you pass `barcode` and another drug already uses it ⇒ `409 Conflict`.
- If `drugId` is omitted, the server generates one as `DRG-{8-char-hex}` from a fresh GUID (e.g. `DRG-A1B2C3D4`) — total length 12 chars.
- `barcodeType` defaults to `"EAN-13"` if blank.
- `categoryName` is **denormalised onto the drug** at create time (snapshot), so renaming a category later won't propagate to existing drugs.
- Beyond the four required fields above, the handler is **lenient** — packaging math (`levelNumber` sequencing, `quantityPerParent` consistency, exactly one `isDefault`) is **not** validated. Garbage in, garbage on the receipt — get the hierarchy right at create time.

### Packaging field cheat sheet

| Field | What it means |
|-------|---------------|
| `levelNumber` | 1 = base (smallest), N = outermost. Must be sequential. |
| `baseUnitQuantity` | How many base units (level 1) make up this level. Strip=10, Box=100. |
| `quantityPerParent` | How many of this level fit in the next one up. (Strip→Box: 10 strips per box.) |
| `isSellable` | Can a cashier ring up this level? Level 1 is often `false` (no loose tablets). |
| `isDefault` | The default sell unit for the till. Only **one** level may set this. |
| `isBreakable` | Can stock be split into smaller levels? A non-breakable Box must sell whole. |
| `barcode` | Optional level-specific barcode for scanning a strip vs a box. |
| `minimumSaleQuantity` | Lower bound for the cashier (e.g. 1 box minimum). |

For deeper packaging math (price-per-unit conversions, FIFO across levels), see [`../packaging/system-guide.md`](../packaging/system-guide.md).

### Step 3 — Browse / search

```http
GET /api/drugs/browse?page=1&limit=20&searchTerm=amox&category=Antibiotics&inStock=true
```

`searchTerm` matches **brandName / genericName / barcode / manufacturer** (case-insensitive substring). `inStock` filters down to drugs with at least one shop holding stock.

Response:

```json
{
  "items": [
    {
      "drugId": "DRG-A1B2C3D4",
      "brandName": "Amoxil 500",
      "genericName": "Amoxicillin",
      "manufacturer": "GSK",
      "barcode": "8901234567890",
      "strength": "500mg",
      "form": "Capsule",
      "suggestedRetailPrice": 5.99,
      "totalQuantityInStock": 1240,
      "shopCount": 3,
      "isAvailable": true,
      "requiresPrescription": true,
      "categoryId": "CAT-A1B2C3D4",
      "category": "Antibiotics",
      "categoryColorCode": "#FF5733",
      "primaryImageUrl": "https://cdn.pharma.com/drug/amoxil.jpg"
    }
  ],
  "page": 1,
  "limit": 20,
  "total": 1
}
```

### Step 4 — Drill into one drug

```http
GET /api/drugs/DRG-A1B2C3D4/detail
```

Returns the drug plus a `shopInventorySummaries` array — one entry per shop carrying stock, with quantity, current selling price, nearest expiry, and a low-stock flag. This is the screen the catalog UI shows when a user clicks a drug.

## Shop-specific packaging overrides

The catalog hierarchy is the **default**. A shop can:

- Disable a level (e.g. Shop B doesn't sell loose strips)
- Make a non-breakable level breakable (or vice-versa)
- Override the minimum sale quantity

These overrides live on `ShopInventory` and are managed through `/api/inventory/shops/{shopId}/drugs/{drugId}/packaging-overrides` — covered in [05 — Inventory](./05-inventory-and-stock.md). Catalog data never changes per shop.

## Activation & deletion

- `Category.isActive` — toggle to hide a category. `GET /api/categories?activeOnly=true` (default) excludes inactive ones.
- **Drugs are not deletable.** No soft-delete column, no `DELETE` endpoint. Catalog records are permanent so historical sales/POs keep referential integrity. To stop selling a drug, deactivate its shop inventory or zero its stock.

## Audit trail

Every entity inherits `BaseEntity` and tracks `CreatedAt`, `CreatedBy`, `LastUpdated`, `UpdatedBy`. Always include the JWT — `CreatedBy` is taken from the `nameidentifier` claim.

## Best practices

### Security
- **Keep `POST /api/drugs` and `POST /api/categories` SuperAdmin-only.** They mutate global, shared catalog data — one careless update affects every shop. Don't loosen the policy for "convenience".
- **The catalog endpoints are public read** (`AllowAnonymous`) by design — you want the storefront to render without a login. That means `description`, `sideEffects`, `interactionNotes`, and `imageUrls` are world-readable; don't include internal commercial notes.
- **Sanitise `imageUrls` on input.** They go straight into responses and (eventually) onto receipts/labels — `javascript:` URLs or attacker-controlled hosts are an XSS / phishing surface for any browser-based client.
- **Validate `barcode` format if compliance requires it.** The handler stores whatever you send; uniqueness is enforced (409) but format isn't.

### Performance
- **Use `/api/drugs/browse` for catalog grids and search** — it returns the slim `DrugListItemDto` (image URL, primary stock, category colour). The full `/api/drugs/{id}` response includes regulatory + side effects + interaction notes and is meant for detail screens.
- **`/api/drugs/{id}/detail` joins across every shop's inventory** to populate `shopInventorySummaries[]`. Cheap with two shops, expensive with hundreds — call only when the user opens a drug detail page.
- **Cache categories aggressively client-side.** They change rarely; `GET /api/categories?activeOnly=true` is fine to fetch once at app start.
- **Pagination is `page` + `limit`** (not `pageSize`). Cap `limit` in the client (e.g. ≤ 100); the API doesn't currently enforce a maximum.

### Correctness
- **Validate the packaging hierarchy yourself before submitting.** The handler is lenient — bad input persists silently. Hard rules:
  - `levelNumber` must be sequential starting at 1.
  - `baseUnitQuantity` of level N must equal `baseUnitQuantity[N-1] × quantityPerParent[N]`.
  - Exactly one level should set `isDefault: true`.
  - Level 1 should usually be `isSellable: false` for non-divisible drugs (you don't sell loose tablets out of a bottle).
- **Set `barcode` per packaging level, not just at the drug level**, when each level has its own scannable code. Outer cartons usually have a different EAN to the inner box.
- **Don't depend on `categoryName` updating after a category rename** — it's snapshotted onto every drug at create time. To "rename" propagates only via re-creating the drugs (or an out-of-band SQL update).
- **Drugs are not deletable.** No `DELETE` endpoint, no `isActive` flag. Plan accordingly: use `ShopInventory` to control whether a shop sells a drug, not the catalog.

### Clean code
- **Send IDs explicitly in your client (not auto-generated).** If you control the `drugId` and `categoryId`, you can deduplicate cleanly across staging/prod and avoid orphan `DRG-…` collisions in fixtures.
- **Prefer `categoryId` over `categoryName` everywhere in client code.** The API accepts both as filters but only the ID is stable.
- **Treat `basePricing.suggestedRetailPrice` as a *hint*** — a vendor recommendation. Cashiers always see `ShopInventory.shopPricing.sellingPrice`. Don't display the catalog suggestion next to the till price; it confuses staff.

## Next

→ [04 — Suppliers & Purchase Orders](./04-suppliers-and-purchase-orders.md)
