## Item Lifecycle & Scenario Guide

This walkthrough covers the end-to-end process for introducing a new drug into the multi-tenant POS, configuring packaging overrides, and handling common real‑world scenarios. Each step lists the primary API endpoint(s) you can call from Postman or your integration tests.

---

### 1. Create the Shop (if not already registered)

Most environments seed shops, but if you need to create one manually:

```http
POST /api/admin/shops
Content-Type: application/json

{
  "shopId": "SHOP-001",
  "name": "Main Street Pharmacy",
  "email": "owner@mainstreet.example",
  "phone": "+1-555-123-4567",
  "address": {
    "street": "123 Main St",
    "city": "Springfield",
    "state": "IL",
    "country": "USA",
    "zipCode": "62701"
  }
}
```

*Response:* details of the new shop with timestamps.

---

### 2. Register the Drug in the Shared Catalog

Define global packaging, formulation, and descriptive data. Global packaging should include base unit and hierarchy (e.g., Tablet → Strip → Box).

```http
POST /api/admin/drugs
Content-Type: application/json

{
  "drugId": "PARA-500-TAB",
  "barcode": "6223001234001",
  "barcodeType": "EAN-13",
  "brandName": "Paracetamol 500mg",
  "genericName": "Paracetamol",
  "manufacturer": "HealthCorp",
  "originCountry": "USA",
  "category": "Analgesics",
  "formulation": {
    "form": "Tablet",
    "strength": "500mg",
    "routeOfAdministration": "Oral"
  },
  "basePricing": {
    "suggestedRetailPrice": 20.00,
    "currency": "USD",
    "suggestedTaxRate": 5.0
  },
  "packagingInfo": {
    "unitType": "Count",
    "baseUnit": "tablet",
    "baseUnitDisplayName": "Tablet",
    "isSubdivisible": true,
    "packagingLevels": [
      {
        "packagingLevelId": "PKG-LV-TABLET",
        "levelNumber": 1,
        "unitName": "Tablet",
        "baseUnitQuantity": 1,
        "quantityPerParent": 1,
        "isSellable": true,
        "isDefault": false
      },
      {
        "packagingLevelId": "PKG-LV-STRIP",
        "levelNumber": 2,
        "unitName": "Strip",
        "baseUnitQuantity": 10,
        "quantityPerParent": 10,
        "parentPackagingLevelId": "PKG-LV-TABLET",
        "isSellable": true,
        "isDefault": true
      },
      {
        "packagingLevelId": "PKG-LV-BOX",
        "levelNumber": 3,
        "unitName": "Box",
        "baseUnitQuantity": 100,
        "quantityPerParent": 10,
        "parentPackagingLevelId": "PKG-LV-STRIP",
        "isSellable": true
      }
    ]
  }
}
```

*Response:* the persisted drug record with catalog metadata.

---

### 3. Attach the Drug to the Shop Inventory

Create a `ShopInventory` entry so the shop can stock and sell the item.

```http
POST /api/inventory/shops/SHOP-001/stock
Content-Type: application/json

{
  "drugId": "PARA-500-TAB",
  "batchNumber": "BATCH-2025-001",
  "supplierId": "SUP-100",
  "quantity": 1000,
  "expiryDate": "2025-12-31",
  "purchasePrice": 12.50,
  "sellingPrice": 18.00,
  "storageLocation": "Warehouse A"
}
```

This call:
1. Creates the `ShopInventory` row if it doesn’t exist.
2. Adds a batch in FIFO order.
3. Updates `TotalStock`, `LastRestockDate`, and availability flags.

---

### 4. Review Default Effective Packaging

Before customizing, check the merged view (global + shop pricing):

```http
GET /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging
```

*Response:* structure showing each level, effective quantities, price fallback, and sellability. Use this snapshot to confirm the default sell unit and base conversions.

---

### 5. Apply Shop-Specific Packaging Overrides

#### Scenario A: Adjust Strip contents and price

A hospital packs strips with 12 tablets instead of 10 and wants a unique price with the strip as the default sell unit.

```http
POST /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging-overrides
Content-Type: application/json

{
  "packagingLevelId": "PKG-LV-STRIP",
  "overrideQuantityToParentLevel": 12,
  "sellingPrice": 5.50,
  "isSellable": true,
  "isDefaultSellUnit": true,
  "sourceType": "manual"
}
```

#### Scenario B: Add a custom promo pack

```http
POST /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging-overrides
Content-Type: application/json

{
  "customUnitName": "Promo Pack",
  "parentPackagingLevelId": "PKG-LV-STRIP",
  "overrideQuantityToParentLevel": 5,
  "sellingPrice": 27.50,
  "minimumSaleQuantity": 1,
  "customLevelOrder": 200,
  "sourceType": "system"
}
```

---

### 6. Update Overrides When Requirements Change

#### Scenario C: Update promo pack pricing

```http
PUT /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging-levels/OVR-PROMO-PACK
Content-Type: application/json

{
  "sellingPrice": 24.99,
  "isSellable": true,
  "overrideQuantityToParentLevel": 5,
  "sourceType": "manual"
}
```

> Use the `overrideId` returned from the GET call. The system recalculates the effective snapshot immediately.

---

### 7. Optionally Remove Overrides

Delete a custom or redundant override after assigning an alternative default:

```http
DELETE /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/packaging-levels/OVR-PROMO-PACK
```

- Allowed: removing custom levels, non-default overrides.
- Blocked: removing the only default sell unit or global catalog definitions.

---

### 8. Update Shop Pricing Structure (Optional)

If the shop wants to tweak the base pricing object:

```http
PUT /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/pricing
Content-Type: application/json

{
  "costPrice": 12.50,
  "sellingPrice": 18.75,
  "taxRate": 5.0
}
```

This adjusts `ShopPricing` (default fallback for packaging prices).

---

### 9. Reduce Stock When Orders Are Fulfilled

When an order sells 3 strips:

```http
PUT /api/inventory/shops/SHOP-001/drugs/PARA-500-TAB/reduce
Content-Type: application/json

{
  "quantity": 3
}
```

The command uses the effective packaging to convert strip → tablet base units and update FIFO batches.

---

### 10. Key Scenario Summary

| Scenario | Description | API(s) |
| --- | --- | --- |
| New drug onboarding | Register catalog entry + seed shop inventory | `POST /api/admin/drugs`, `POST /api/inventory/shops/{shopId}/stock` |
| Shop-specific pack quantity change | Override global level with new conversion | `POST /api/inventory/shops/{shopId}/drugs/{drugId}/packaging-overrides` |
| Custom promotional bundle | Add custom packaging level with price | Same as above (custom payload) |
| Pricing adjustments | Update override or shop pricing defaults | `PUT /packaging-levels/{levelId}`, `PUT /pricing` |
| Removing temporary packs | Delete override after promotion ends | `DELETE /packaging-levels/{overrideId}` |
| Quick check for POS | Retrieve effective packaging snapshot | `GET /packaging` |

---

### 11. Effective Packaging Snapshot Notes

- Every create/update/delete of an override triggers an immediate rebuild of the cached effective snapshot to keep POS queries fast and consistent.
- If you automate promotions or bulk imports, schedule them during low-traffic windows because the snapshot rebuild is synchronous.

---

### 12. Tips for Testing & Automation

1. **Seed scripts:** Use the same HTTP flow in integration tests to mimic a real setup.
2. **Validation:** After each change, call `GET /packaging` to verify effective quantities and prices.
3. **Rollbacks:** Avoid deleting overrides without first ensuring another default sell unit is set—otherwise sales commands will reject transactions.
4. **Audit tracking:** Include `sourceType` and track `lastUpdatedBy/At` from responses for operations dashboards.

---

With these steps you can confidently introduce new items, customize packaging per tenant, and keep inventory math consistent across sales, stock adjustments, and reporting. Feel free to adapt the sample payloads to your environment or automation scripts.
