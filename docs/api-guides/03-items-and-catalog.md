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

This is the heaviest payload in the API. Build it in three logical pieces: identity, regulatory/formulation, and the **packaging hierarchy**.

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

## Next

→ [04 — Suppliers & Purchase Orders](./04-suppliers-and-purchase-orders.md)
