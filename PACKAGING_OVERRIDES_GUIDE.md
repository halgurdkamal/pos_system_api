## Shop Packaging Overrides Guide

This guide explains how the hybrid packaging model works for shops that need to tweak packaging hierarchies, pricing, or sellability while keeping the catalog’s global definitions intact.

### Implementation Notes & Compatibility

- **Entity updates**: add a `SourceType` column (enum/string) to `ShopPackagingOverride` plus expose the existing `UpdatedBy`/`LastUpdated` fields via the API as `lastUpdatedBy` and `lastUpdatedAt`. The new column plugs into the current migration strategy without breaking existing rows (default to `manual`).
- **Service layer**: when persisting overrides, populate `SourceType` (`manual`, `system`, `imported`) and map audit data to the new response fields. No controller routing changes are required.
- **Field rename (alias)**: the stored property `OverrideQuantityPerParent` remains but the API/documentation surface now prefers the alias `overrideQuantityToParentLevel`. Keep both names deserializable to maintain backward compatibility.
- **Effective snapshot**: after create/update/delete operations, the `EffectivePackagingService` should continue to recompute the cached snapshot so that POS queries stay fast.

### Concepts

- **Global packaging levels** live on the `Drug` catalog entry and define the canonical hierarchy (e.g., `Box -> Strip -> Tablet`). Every level has a `packagingLevelId`, `unitName`, and base-unit math.
- **Shop packaging overrides** sit on `ShopInventory` and can either:
  - Override a global level by referencing its `packagingLevelId`; or
  - Introduce a fully custom level when `packagingLevelId` is omitted.
- Overrides can provide custom `overrideQuantityToParentLevel`, `sellingPrice`, `minimumSaleQuantity`, `isSellable`, and `isDefaultSellUnit`. When fields are left null, the system falls back to the global definition or shop pricing defaults.
- Every override must still resolve to the same base unit as the catalog drug so stock math stays consistent.

### New Fields / Optional Fields

- `sourceType` *(enum: manual | imported | system)* — describes the origin of the override for audit/traceability.
- `lastUpdatedBy`, `lastUpdatedAt` — surfaced from `UpdatedBy`/`LastUpdated` on the entity to show who touched the override last.
- `minimumSaleQuantity` — remains shop-specific; when omitted the system falls back to the global or shop-level baseline. Include it in POST/PUT only when the shop needs a different minimum.
- `overrideQuantityToParentLevel` — preferred request/response name; legacy clients may continue to send `overrideQuantityPerParent`.

### Key Endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/inventory/shops/{shopId}/drugs/{drugId}/packaging` | Returns the merged global + shop view. |
| `POST` | `/api/inventory/shops/{shopId}/drugs/{drugId}/packaging-overrides` | Creates a new override (global tweak or custom level). |
| `PUT` | `/api/inventory/shops/{shopId}/drugs/{drugId}/packaging-levels/{levelId}` | Updates an existing override or applies overrides to a global level. |
| `DELETE` | `/api/inventory/shops/{shopId}/drugs/{drugId}/packaging-levels/{overrideId}` | Removes a shop override (custom level or non-default global override). |

### GET Example

```json
{
  "shopId": "SHOP-001",
  "drugId": "PARA-500-TAB",
  "packagingLevels": [
    {
      "levelId": "PKG-LV-BASE",
      "overrideId": null,
      "unitName": "Tablet",
      "isGlobal": true,
      "isSellable": true,
      "isDefaultSellUnit": false,
      "globalBaseUnitQuantity": 1,
      "effectiveBaseUnitQuantity": 1,
      "globalQuantityPerParent": 1,
      "effectiveQuantityPerParent": 1,
      "overrideQuantityToParentLevel": null,
      "sellingPrice": 0.50,
      "minimumSaleQuantity": 10,
      "parentLevelId": null,
      "parentOverrideId": null,
      "sourceType": "manual",
      "lastUpdatedBy": "inventory.manager@shop.com",
      "lastUpdatedAt": "2025-10-29T09:40:11Z"
    },
    {
      "levelId": "PKG-LV-STRIP",
      "overrideId": "OVR-1234",
      "unitName": "Strip",
      "isGlobal": true,
      "isSellable": true,
      "isDefaultSellUnit": true,
      "globalBaseUnitQuantity": 10,
      "effectiveBaseUnitQuantity": 12,
      "globalQuantityPerParent": 10,
      "effectiveQuantityPerParent": 12,
      "overrideQuantityToParentLevel": 12,
      "sellingPrice": 5.20,
      "minimumSaleQuantity": null,
      "parentLevelId": "PKG-LV-BASE",
      "parentOverrideId": null,
      "sourceType": "manual",
      "lastUpdatedBy": "inventory.manager@shop.com",
      "lastUpdatedAt": "2025-10-29T09:40:11Z"
    },
    {
      "levelId": null,
      "overrideId": "OVR-5678",
      "unitName": "Promo Pack",
      "isGlobal": false,
      "isSellable": true,
      "isDefaultSellUnit": false,
      "globalBaseUnitQuantity": 0,
      "effectiveBaseUnitQuantity": 60,
      "globalQuantityPerParent": 0,
      "effectiveQuantityPerParent": 5,
      "overrideQuantityToParentLevel": 5,
      "sellingPrice": 24.99,
      "minimumSaleQuantity": 1,
      "parentLevelId": "PKG-LV-STRIP",
      "parentOverrideId": null,
      "sourceType": "imported",
      "lastUpdatedBy": "bulk-loader@hq",
      "lastUpdatedAt": "2025-10-28T23:15:00Z"
    }
  ]
}
```

### POST Example

Override a global level (e.g., set Strip to 12 tablets and mark it as the default sell unit):

```http
POST /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging-overrides
Content-Type: application/json

{
  "packagingLevelId": "PKG-LV-STRIP",
  "overrideQuantityToParentLevel": 12,
  "sellingPrice": 5.20,
  "isSellable": true,
  "isDefaultSellUnit": true,
  "minimumSaleQuantity": null,
  "sourceType": "manual"
}
```

Create a custom packaging level tied to a global parent (e.g., “Promo Pack” containing 5 strips):

```http
POST /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging-overrides
Content-Type: application/json

{
  "customUnitName": "Promo Pack",
  "parentPackagingLevelId": "PKG-LV-STRIP",
  "overrideQuantityToParentLevel": 5,
  "sellingPrice": 24.99,
  "isSellable": true,
  "minimumSaleQuantity": 1,
  "customLevelOrder": 100,
  "sourceType": "imported"
}
```

### PUT Example

Update an existing custom override (identified by override ID returned from GET):

```http
PUT /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging-levels/OVR-5678
Content-Type: application/json

{
  "sellingPrice": 22.99,
  "isSellable": true,
  "isDefaultSellUnit": false,
  "minimumSaleQuantity": 1,
  "sourceType": "manual",
  "overrideQuantityToParentLevel": 5
}
```

Apply a new default to a global level when no override existed yet (handler creates it automatically):

```http
PUT /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging-levels/PKG-LV-BOX
Content-Type: application/json

{
  "overrideQuantityToParentLevel": 8,
  "isDefaultSellUnit": true,
  "sellingPrice": 40.00,
  "minimumSaleQuantity": null,
  "sourceType": "system"
}
```

### DELETE Example

Delete a shop override (custom level or non-default global override). The API blocks removal if it would leave the shop without any default sell unit or if the override is the only reference protecting stock integrity.

```http
DELETE /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging-levels/OVR-5678
```

- ✅ Allowed: deleting custom levels and global overrides that are not currently marked as the default sell unit (or after another default is set).
- ❌ Not allowed: deleting the only default sell unit for the shop, or removing the global catalog definition.

### Validation Rules

- Only one override per `(shopId, drugId, packagingLevelId)` is allowed.
- Custom levels must supply `customUnitName` and `overrideQuantityToParentLevel > 0` (legacy clients may still send `overrideQuantityPerParent`).
- Provide **either** `parentPackagingLevelId` **or** `parentOverrideId` (not both) to keep the hierarchy consistent.
- Marking a level as `isDefaultSellUnit` automatically clears other defaults for the same shop/drug.
- Every custom override chain must ultimately connect to the drug’s base unit so stock adjustments remain accurate.

Refer to the API controller and service implementations for deeper details:
- `src/API/Controllers/InventoryController.cs`
- `src/Core/Application/Inventory/Services/EffectivePackagingService.cs`

### Effective Packaging Snapshot & Cache

- After every create/update/delete, the system recomputes an **effective packaging snapshot** that flattens global levels plus overrides into the structure returned by `GET /packaging`.
- POS lookups read from this cached snapshot first, dramatically reducing query time for frequently sold items.
- The snapshot rebuild is synchronous with the command handlers so clients always receive up-to-date data on subsequent reads.

### Change Log

- Added audit metadata fields (`sourceType`, `lastUpdatedBy`, `lastUpdatedAt`) and clarified minimum sale quantity handling.
- Introduced the `overrideQuantityToParentLevel` alias while maintaining backward compatibility with legacy payloads.
- Documented DELETE endpoint usage and outlined when deletions are permitted.
- Added notes on the effective packaging snapshot cache and a dedicated optional fields section for quick reference.
