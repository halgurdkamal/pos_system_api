# Drug Packaging System Implementation

## Overview

The POS System now supports a comprehensive **packaging hierarchy system** that allows drugs to be measured, stored, and sold at multiple levels (e.g., tablets, strips, boxes).

## What Was Implemented

### 1. New Value Objects

#### **UnitType Enum** (`src/Core/Domain/Drugs/ValueObjects/UnitType.cs`)

Defines four types of measurement systems:

- **Count**: Discrete items (tablets, capsules, vials)
- **Volume**: Liquids in milliliters (syrups, injections)
- **Weight**: Powders/ointments in grams (creams, gels)
- **Dose**: Metered doses (inhalers, sprays) - cannot be subdivided

#### **PackagingLevel** (`src/Core/Domain/Drugs/ValueObjects/PackagingLevel.cs`)

Represents a single level in the packaging hierarchy:

- `LevelNumber`: 1 (base), 2, 3, etc.
- `UnitName`: "Tablet", "Strip", "Box", "Bottle", etc.
- `BaseUnitQuantity`: How many base units this level contains
- `IsSellable`: Can customers buy at this level?
- `IsDefault`: Is this the default sell unit?
- `IsBreakable`: Can this be subdivided for partial sales?
- `Barcode`: Optional barcode for this specific level
- `MinimumSaleQuantity`: Minimum quantity that can be sold

#### **PackagingInfo** (`src/Core/Domain/Drugs/ValueObjects/PackagingInfo.cs`)

Complete packaging configuration for a drug:

- `UnitType`: The measurement system type
- `BaseUnit`: Base unit symbol ("ml", "tablet", "g")
- `BaseUnitDisplayName`: Human-readable name
- `IsSubdivisible`: Can the base unit be divided?
- `PackagingLevels`: List of all packaging levels
- Helper methods for unit conversion and validation

### 2. Updated Entities

#### **Drug Entity** (`src/Core/Domain/Drugs/Entities/Drug.cs`)

Added:

```csharp
public PackagingInfo PackagingInfo { get; set; } = new();
```

#### **ShopInventory Entity** (`src/Core/Domain/Inventory/Entities/ShopInventory.cs`)

Added:

```csharp
public string? ShopSpecificSellUnit { get; set; }  // Override default sell unit
public decimal? MinimumSaleQuantity { get; set; }   // Shop-level minimum
```

New methods:

- `SetShopSpecificSellUnit(string? unitName)`
- `SetMinimumSaleQuantity(decimal? quantity)`

### 3. EF Core Configuration

#### **DrugConfiguration** (`src/Infrastructure/Data/Configurations/DrugConfiguration.cs`)

Configured PackagingInfo as owned entity:

- `PackagingInfo_UnitType`: Stored as string enum
- `PackagingInfo_BaseUnit`: varchar(50)
- `PackagingInfo_BaseUnitDisplayName`: varchar(100)
- `PackagingInfo_IsSubdivisible`: boolean
- `PackagingInfo_PackagingLevels`: JSONB for flexibility

#### **ShopInventoryConfiguration** (`src/Infrastructure/Data/Configurations/ShopInventoryConfiguration.cs`)

Added columns:

- `ShopSpecificSellUnit`: varchar(50), nullable
- `MinimumSaleQuantity`: decimal(18,2), nullable

### 4. Database Migration

**Migration**: `20251027223742_AddPackagingInfoToDrugs`

Changes:

- Added 5 columns to `Drugs` table for PackagingInfo
- Added 2 columns to `ShopInventory` table for shop overrides
- Set default values for existing drugs (Count type, single Tablet level)

### 5. Sample Data

**File**: `src/Infrastructure/SampleData/DrugPackagingExamples.cs`

Contains 5 real-world examples:

1. **Amoxicillin 500mg Capsules** (Count-based)

   - Level 1: Capsule (1 unit) - sellable
   - Level 2: Strip (10 capsules) - **default**, breakable
   - Level 3: Box (100 capsules) - sellable, breakable

2. **Paracetamol Syrup** (Volume-based)

   - Level 1: ml (1 unit) - sellable, min 5ml
   - Level 2: Bottle (120ml) - **default**, breakable
   - Level 3: Box (1200ml / 10 bottles) - sellable

3. **Hydrocortisone Cream** (Weight-based)

   - Level 1: Gram (1 unit) - NOT sellable
   - Level 2: Tube (15g) - **default**, NOT breakable
   - Level 3: Box (75g / 5 tubes) - sellable

4. **Ventolin Inhaler** (Dose-based)

   - Level 1: Puff (1 unit) - NOT sellable
   - Level 2: Inhaler (200 puffs) - **default**, NOT breakable
   - Level 3: Box (1 inhaler) - sellable

5. **Aspirin 100mg Tablets** (Flexible packaging)
   - Level 1: Tablet (1 unit) - sellable
   - Level 2: Strip (10 tablets) - sellable
   - Level 3: Box (100 tablets) - **default** (OTC drug)

## How It Works

### Multi-Tenant Default Sell Unit

**Priority Order**:

1. Check `ShopInventory.ShopSpecificSellUnit` (shop override)
2. If null → Use `Drug.PackagingInfo.GetDefaultSellUnit()` (central catalog)
3. Validate that level `IsSellable = true`

**Example Flow**:

```
Drug: Amoxicillin → Default = "Strip"
Shop A: ShopSpecificSellUnit = "Box" (hospital pharmacy buys bulk)
Shop B: ShopSpecificSellUnit = null (uses central default "Strip")

POS Behavior:
- Scan barcode in Shop A → Shows "1 Box"
- Scan barcode in Shop B → Shows "1 Strip"
- Both can override at sale time if needed
```

### Stock Tracking (Base Units)

All inventory is tracked in **base units**:

```csharp
ShopInventory:
- TotalQuantityInBaseUnits: 1000 (tablets)

Can represent:
- 1000 tablets
- 100 strips (100 × 10)
- 10 boxes (10 × 100)
- 5 boxes + 50 strips
```

**Benefits**:

- ✅ Accurate stock calculations
- ✅ No rounding errors
- ✅ Flexible sales at any packaging level
- ✅ Consistent batch tracking

### Unit Conversion

Built-in conversion methods:

```csharp
var packaging = drug.PackagingInfo;

// Convert 5 strips to tablets
var tablets = packaging.ConvertQuantity(5, "Strip", "Tablet");
// Result: 50 tablets

// Convert 75 tablets to strips
var strips = packaging.ConvertQuantity(75, "Tablet", "Strip");
// Result: 7.5 strips (can sell if IsBreakable = true)
```

### Validation Rules

PackagingInfo has built-in validation:

```csharp
var (isValid, errors) = drug.PackagingInfo.Validate();

Checks:
✓ At least one packaging level exists
✓ Base level (LevelNumber = 1) is present
✓ Only ONE level marked as default
✓ Default level is sellable
✓ Level numbers are sequential (1, 2, 3...)
✓ All levels have BaseUnitQuantity > 0
```

## API Usage Examples

### Get Drug with Packaging Info

```http
GET /api/drugs/{drugId}

Response:
{
  "drugId": "AMOX-500-CAP",
  "brandName": "Amoxil",
  "packagingInfo": {
    "unitType": "Count",
    "baseUnit": "capsule",
    "baseUnitDisplayName": "Capsule",
    "isSubdivisible": true,
    "packagingLevels": [
      {
        "levelNumber": 1,
        "unitName": "Capsule",
        "baseUnitQuantity": 1,
        "isSellable": true,
        "isDefault": false,
        "isBreakable": false
      },
      {
        "levelNumber": 2,
        "unitName": "Strip",
        "baseUnitQuantity": 10,
        "isSellable": true,
        "isDefault": true,
        "isBreakable": true,
        "barcode": "6223001234574"
      },
      {
        "levelNumber": 3,
        "unitName": "Box",
        "baseUnitQuantity": 100,
        "isSellable": true,
        "isDefault": false,
        "isBreakable": true,
        "barcode": "6223001234581"
      }
    ]
  }
}
```

### Get Shop Inventory with Effective Default

```http
GET /api/inventory/shop/{shopId}/drug/{drugId}

Response:
{
  "shopId": "SH-001",
  "drugId": "AMOX-500-CAP",
  "totalStock": 1000,  // Base units (capsules)
  "shopSpecificSellUnit": "Box",  // Shop override
  "effectiveDefaultSellUnit": "Box",  // Resolved default
  "availableStock": {
    "boxes": 10,
    "strips": 100,
    "capsules": 1000
  }
}
```

### Set Shop-Specific Default

```http
PATCH /api/inventory/{inventoryId}/default-unit

Body:
{
  "shopSpecificSellUnit": "Box",
  "minimumSaleQuantity": 1
}
```

## Business Rules by Unit Type

### Count-Based (Tablets, Capsules)

- ✅ Can break strips if `IsBreakable = true`
- ✅ Track loose tablets separately
- ⚠️ Broken strips have shorter shelf life
- 📝 Label requirements for loose dispensing

### Volume-Based (Syrups, Liquids)

- ✅ Can sell partial bottles if breakable
- ⚠️ Requires measurement tools (graduated cylinders)
- 📝 Must provide correct dosage instructions
- 🔄 Track opened bottles for expiry

### Weight-Based (Creams, Ointments)

- ❌ Usually cannot break tubes (contamination risk)
- ⚠️ Sell whole tubes/jars only
- 📝 Temperature-sensitive storage
- 🔄 After opening: Limited use period

### Dose-Based (Inhalers, Sprays)

- ❌ Cannot break devices (sealed units)
- ⚠️ Sell whole units only
- 📝 Provide device usage instructions
- 🔄 No partial sales allowed

## Next Steps

### Recommended Implementations

1. **Application Layer** (DTOs & Handlers)

   - Create `PackagingInfoDto` for API responses
   - Add `GetEffectiveDefaultSellUnit` query
   - Add `SetShopSpecificSellUnit` command
   - Update `GetDrugQuery` to include packaging

2. **Sales Order Integration**

   - Modify `SalesOrderLine` to include `PackagingLevelSold`
   - Auto-calculate `BaseUnitsConsumed` from quantity + level
   - Validate stock availability in base units
   - Support multi-level sales (2 boxes + 3 strips)

3. **POS Controller**

   - Add endpoint to get sellable units for a drug
   - Add endpoint to convert quantities between units
   - Add barcode scanning support for packaging levels
   - Smart suggestions for quantity (e.g., "25 tablets = 2 strips + 5 loose")

4. **Purchase Order Integration**

   - Allow receiving inventory at any packaging level
   - Convert to base units for stock updates
   - Track supplier packaging preferences

5. **UI Enhancements**
   - Dropdown for packaging level selection
   - Real-time unit conversion display
   - Visual packaging hierarchy
   - Stock availability per level

## Testing the Implementation

### 1. Check Migration Applied

```bash
dotnet ef migrations list
# Should show: 20251027223742_AddPackagingInfoToDrugs (Applied)
```

### 2. Use Sample Data

```csharp
// In your seed/test data
var drugs = DrugPackagingExamples.GetAllExamples();
await _drugRepository.AddRangeAsync(drugs);
```

### 3. Test Queries

```sql
-- View packaging info
SELECT
    "DrugId",
    "BrandName",
    "PackagingInfo_UnitType",
    "PackagingInfo_BaseUnit",
    "PackagingInfo_PackagingLevels"
FROM "Drugs"
WHERE "DrugId" = 'AMOX-500-CAP';

-- View shop overrides
SELECT
    "ShopId",
    "DrugId",
    "TotalStock",
    "ShopSpecificSellUnit",
    "MinimumSaleQuantity"
FROM "ShopInventory";
```

## Key Benefits

✅ **Flexible Sales**: Sell at any packaging level  
✅ **Accurate Inventory**: Base unit tracking prevents errors  
✅ **Multi-Tenant Support**: Shop-specific defaults + central catalog  
✅ **Regulatory Compliance**: Track exact quantities  
✅ **Scalable**: Works for all drug types (solids, liquids, inhalers)  
✅ **Business Intelligence**: Track which units sell most  
✅ **Pricing Flexibility**: Different prices per packaging level

## Schema Changes Summary

### Drugs Table

- Added `PackagingInfo_UnitType` (text)
- Added `PackagingInfo_BaseUnit` (varchar 50)
- Added `PackagingInfo_BaseUnitDisplayName` (varchar 100)
- Added `PackagingInfo_IsSubdivisible` (boolean)
- Added `PackagingInfo_PackagingLevels` (jsonb)

### ShopInventory Table

- Added `ShopSpecificSellUnit` (varchar 50, nullable)
- Added `MinimumSaleQuantity` (decimal 18,2, nullable)

## Files Modified/Created

### New Files

1. `src/Core/Domain/Drugs/ValueObjects/UnitType.cs`
2. `src/Core/Domain/Drugs/ValueObjects/PackagingLevel.cs`
3. `src/Core/Domain/Drugs/ValueObjects/PackagingInfo.cs`
4. `src/Infrastructure/SampleData/DrugPackagingExamples.cs`
5. `src/Infrastructure/Data/Migrations/20251027223742_AddPackagingInfoToDrugs.cs`

### Modified Files

1. `src/Core/Domain/Drugs/Entities/Drug.cs`
2. `src/Core/Domain/Inventory/Entities/ShopInventory.cs`
3. `src/Infrastructure/Data/Configurations/DrugConfiguration.cs`
4. `src/Infrastructure/Data/Configurations/ShopInventoryConfiguration.cs`

---

**Implementation Date**: October 28, 2025  
**Migration**: 20251027223742_AddPackagingInfoToDrugs  
**Status**: ✅ Applied to database
