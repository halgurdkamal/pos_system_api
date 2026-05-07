# Batch Auto-Switch & Global Packaging Price Update Demo

## System Overview

Your pharmacy POS system now has a complete **automatic batch switching and pricing update** system:

1. **Global Drug Packaging** - Defined once per drug (Box → Strip → Tablet)
2. **Shop-Specific Inventory** - Each shop has its own batches and pricing
3. **FIFO Batch Management** - Old batches sold first (First In, First Out)
4. **Automatic Price Updates** - When old batch exhausted, new batch becomes active and prices auto-update

## Complete Workflow Example

### Initial Setup

```json
// Drug in Global Catalog
{
  "drugId": "DRUG-PARACETAMOL-500",
  "brandName": "Panadol",
  "packagingInfo": {
    "packagingLevels": [
      {
        "unitName": "Box",
        "quantityPerParent": 20,    // 20 strips per box
        "baseUnitQuantity": 200     // 20 × 10 = 200 tablets
      },
      {
        "unitName": "Strip",
        "quantityPerParent": 10,    // 10 tablets per strip
        "baseUnitQuantity": 10
      },
      {
        "unitName": "Tablet",
        "quantityPerParent": 1,     // Base unit
        "baseUnitQuantity": 1
      }
    ]
  }
}

// Shop Inventory with Multiple Batches
{
  "shopId": "SHOP-001",
  "drugId": "DRUG-PARACETAMOL-500",
  "totalStock": 1500,
  "batches": [
    {
      "batchNumber": "BATCH-OLD",
      "receivedDate": "2025-10-01",
      "quantityOnHand": 500,         // ← Active (oldest)
      "sellingPrice": 0.50,          // Old price
      "status": "Active"
    },
    {
      "batchNumber": "BATCH-NEW",
      "receivedDate": "2025-10-20",
      "quantityOnHand": 1000,        // Waiting
      "sellingPrice": 0.60,          // New price
      "status": "Active"
    }
  ],
  "shopPricing": {
    "packagingLevelPrices": {
      "Box": 0,        // Auto-calculate from active batch
      "Strip": 5.50,   // Custom shop price (override)
      "Tablet": 0      // Auto-calculate from active batch
    }
  }
}
```

### Step 1: Initial State (Old Batch Active)

**Active Batch:** BATCH-OLD (500 tablets @ $0.50 each)

**Current Prices:**

```json
{
  "Box": 100.0, // Auto: 0.50 × 200 = $100.00
  "Strip": 5.5, // Custom: Shop override
  "Tablet": 0.5 // Auto: 0.50 × 1 = $0.50
}
```

**API Call:**

```http
GET /api/inventory/shops/SHOP-001/drugs/DRUG-PARACETAMOL-500/packaging-pricing
```

**Response:**

```json
{
  "packagingLevelPrices": {
    "Box": 100.0,
    "Strip": 5.5,
    "Tablet": 0.5
  },
  "lastPriceUpdate": "2025-10-25T10:00:00Z"
}
```

### Step 2: Customer Purchases (Old Batch Being Sold)

**Sales Transaction:**

```http
POST /api/sales/orders
{
  "items": [
    {
      "drugId": "DRUG-PARACETAMOL-500",
      "packagingLevel": "Box",
      "quantity": 2,
      "price": 100.00
    }
  ]
}
```

**Effect:**

- 400 tablets sold (2 boxes × 200 tablets)
- BATCH-OLD: 500 → 100 tablets remaining
- BATCH-NEW: Still waiting at 1000 tablets

### Step 3: Old Batch Exhausted

**Final Sale from Old Batch:**

```http
POST /api/sales/orders
{
  "items": [
    {
      "drugId": "DRUG-PARACETAMOL-500",
      "packagingLevel": "Tablet",
      "quantity": 100,
      "price": 0.50
    }
  ]
}
```

**Result:**

- BATCH-OLD: 100 → 0 tablets (EXHAUSTED!)
- BATCH-NEW: Now becomes active (FIFO - First In, First Out)

**New Active Batch:** BATCH-NEW (1000 tablets @ $0.60 each)

### Step 4: Automatic Price Update

Now you need to update packaging prices to reflect the new batch:

```http
POST /api/inventory/shops/SHOP-001/drugs/DRUG-PARACETAMOL-500/packaging-pricing/update-from-batch
Authorization: Bearer {token}
```

**What Happens:**

1. System detects BATCH-NEW is now active (FIFO)
2. Reads new selling price: $0.60 per tablet
3. Updates packaging level prices:
   - Box: 0 → **Auto-calculate:** 0.60 × 200 = $120.00 ✓
   - Strip: 5.50 → **Keep custom:** 5.50 (no change) ✓
   - Tablet: 0 → **Auto-calculate:** 0.60 × 1 = $0.60 ✓

**Response:**

```json
{
  "updatedPricing": {
    "packagingLevelPrices": {
      "Box": 120.0, // ↑ Changed from 100.00
      "Strip": 5.5, // → Unchanged (custom)
      "Tablet": 0.6 // ↑ Changed from 0.50
    },
    "lastPriceUpdate": "2025-11-01T14:30:00Z"
  },
  "summary": {
    "batchNumber": "BATCH-NEW",
    "batchSellingPrice": 0.6,
    "totalLevels": 3,
    "autoCalculatedCount": 2,
    "addedCount": 0,
    "customPriceCount": 1,
    "changes": [
      {
        "unitName": "Box",
        "oldPrice": 100.0,
        "newPrice": 120.0,
        "effectiveBaseUnitQuantity": 200,
        "changeType": "AutoCalculated",
        "calculationFormula": "0.60 (batch) × 200.00 (base units) = 120.00"
      },
      {
        "unitName": "Strip",
        "oldPrice": 5.5,
        "newPrice": 5.5,
        "effectiveBaseUnitQuantity": 10,
        "changeType": "CustomPriceKept",
        "calculationFormula": "Custom shop price retained"
      },
      {
        "unitName": "Tablet",
        "oldPrice": 0.5,
        "newPrice": 0.6,
        "effectiveBaseUnitQuantity": 1,
        "changeType": "AutoCalculated",
        "calculationFormula": "0.60 (batch) × 1.00 (base units) = 0.60"
      }
    ]
  }
}
```

### Step 5: Verify New Prices

```http
GET /api/inventory/shops/SHOP-001/drugs/DRUG-PARACETAMOL-500/packaging-pricing
```

**Response:**

```json
{
  "costPrice": 0.4,
  "sellingPrice": 0.6,
  "packagingLevelPrices": {
    "Box": 120.0, // ← Updated
    "Strip": 5.5, // ← Kept custom
    "Tablet": 0.6 // ← Updated
  },
  "lastPriceUpdate": "2025-11-01T14:30:00Z"
}
```

## Visual Workflow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ BATCH LIFECYCLE & AUTOMATIC PRICING                         │
└─────────────────────────────────────────────────────────────┘

Phase 1: TWO BATCHES IN STOCK
┌──────────────────────────────────────────────────────┐
│ BATCH-OLD (Oct 1)                                    │
│ ✓ Active (FIFO)  │  500 tablets  │  $0.50 each     │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│ Packaging Prices Based on BATCH-OLD:                 │
│ • Box: $100.00 (0.50 × 200)                          │
│ • Strip: $5.50 (custom - kept)                       │
│ • Tablet: $0.50 (0.50 × 1)                           │
└──────────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────────┐
│ BATCH-NEW (Oct 20)                                   │
│ ○ Waiting       │  1000 tablets │  $0.60 each      │
└──────────────────────────────────────────────────────┘

                    ↓ Sales happen ↓

Phase 2: OLD BATCH SOLD OUT
┌──────────────────────────────────────────────────────┐
│ BATCH-OLD (Oct 1)                                    │
│ ✗ EXHAUSTED     │  0 tablets    │  $0.50 each      │
└──────────────────────────────────────────────────────┘
┌──────────────────────────────────────────────────────┐
│ BATCH-NEW (Oct 20)                                   │
│ ✓ Active (FIFO)  │  1000 tablets │  $0.60 each     │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│ ⚠️  PRICES NOT YET UPDATED - Still showing $0.50    │
└──────────────────────────────────────────────────────┘

         ↓ POST .../update-from-batch ↓

Phase 3: PRICES AUTO-UPDATED
┌──────────────────────────────────────────────────────┐
│ BATCH-NEW (Oct 20)                                   │
│ ✓ Active (FIFO)  │  1000 tablets │  $0.60 each     │
│ ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│ Packaging Prices Updated to BATCH-NEW:               │
│ • Box: $120.00 (0.60 × 200) ← UPDATED               │
│ • Strip: $5.50 (custom) ← KEPT                       │
│ • Tablet: $0.60 (0.60 × 1) ← UPDATED                │
└──────────────────────────────────────────────────────┘
```

## Integration Scenarios

### Scenario A: Manual Trigger (After Sales)

```javascript
// In your POS application
async function onSaleCompleted(saleId) {
  const sale = await getSale(saleId);

  // Check if any items might have exhausted batches
  for (const item of sale.items) {
    const inventory = await getInventory(item.shopId, item.drugId);

    if (inventory.totalStock < inventory.reorderPoint) {
      // Low stock - might have switched batches
      await updatePackagingPrices(item.shopId, item.drugId);
      console.log(`Updated pricing for ${item.drugId} due to low stock`);
    }
  }
}
```

### Scenario B: Scheduled Update (Daily)

```javascript
// Run nightly to ensure all pricing is current
async function updateAllPricingNightly() {
  const allShops = await getShops();

  for (const shop of allShops) {
    const inventory = await getShopInventory(shop.id);

    for (const item of inventory) {
      try {
        await updatePackagingPrices(shop.id, item.drugId);
      } catch (error) {
        console.error(`Failed to update ${item.drugId}:`, error);
      }
    }
  }
}
```

### Scenario C: Webhook on Batch Exhaustion

```csharp
// In your inventory service
public async Task OnBatchExhausted(string shopId, string drugId, string batchNumber)
{
    _logger.LogInformation(
        "Batch {Batch} exhausted for Drug {Drug} in Shop {Shop}. Updating pricing...",
        batchNumber, drugId, shopId);

    // Trigger automatic price update
    var command = new UpdatePackagingPricesFromBatchCommand(shopId, drugId);
    var result = await _mediator.Send(command);

    if (result.HasChanges)
    {
        // Notify POS terminals
        await _notificationService.NotifyPriceChange(shopId, drugId, result.Summary);

        // Log for audit
        await _auditService.LogPriceUpdate(shopId, drugId, result);
    }
}
```

## Multi-Shop Example

Different shops can have different custom pricing:

### Shop A (Premium Location)

```json
{
  "shopId": "SHOP-PREMIUM",
  "packagingLevelPrices": {
    "Box": 150.0, // Custom premium markup
    "Strip": 0, // Auto from batch
    "Tablet": 0 // Auto from batch
  }
}
```

**After update:**

```json
{
  "Box": 150.0, // KEPT (custom)
  "Strip": 6.0, // UPDATED (0.60 × 10)
  "Tablet": 0.6 // UPDATED (0.60 × 1)
}
```

### Shop B (Budget Location)

```json
{
  "shopId": "SHOP-BUDGET",
  "packagingLevelPrices": {
    "Box": 0, // Auto from batch
    "Strip": 0, // Auto from batch
    "Tablet": 0.55 // Custom discount
  }
}
```

**After update:**

```json
{
  "Box": 120.0, // UPDATED (0.60 × 200)
  "Strip": 6.0, // UPDATED (0.60 × 10)
  "Tablet": 0.55 // KEPT (custom discount)
}
```

## Key Benefits

### ✅ Automatic Pricing

- No manual price updates needed
- Always reflects current batch cost
- Maintains profit margins automatically

### ✅ Shop Flexibility

- Each shop can override specific levels
- Custom pricing preserved during updates
- Support promotional pricing

### ✅ FIFO Batch Management

- Oldest stock sold first
- Automatic batch switching
- Reduces expiry waste

### ✅ Global Packaging

- Define packaging once per drug
- All shops use same hierarchy
- Consistent across organization

### ✅ Audit Trail

- Every change tracked
- Before/after prices logged
- Calculation formulas documented

## Testing Your System

### Test 1: Basic Batch Switch

```bash
# 1. Check current active batch
GET /api/inventory/shops/SHOP-001/drugs/DRUG-001

# 2. Sell all from old batch
POST /api/sales/orders
{
  "items": [{ "drugId": "DRUG-001", "quantity": 500 }]
}

# 3. Update prices (new batch now active)
POST /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing/update-from-batch

# 4. Verify prices changed
GET /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing
```

### Test 2: Custom Price Preservation

```bash
# 1. Set custom price
PUT /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing
{
  "Box": 200.00,  # Custom high price
  "Strip": 0,
  "Tablet": 0
}

# 2. Update from batch
POST /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing/update-from-batch

# 3. Verify Box price kept at 200.00
GET /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing
# Should show: Box = 200.00 (unchanged)
```

### Test 3: Multi-Shop Different Pricing

```bash
# Update both shops
POST /api/inventory/shops/SHOP-A/drugs/DRUG-001/packaging-pricing/update-from-batch
POST /api/inventory/shops/SHOP-B/drugs/DRUG-001/packaging-pricing/update-from-batch

# Compare results
GET /api/inventory/shops/SHOP-A/drugs/DRUG-001/packaging-pricing
GET /api/inventory/shops/SHOP-B/drugs/DRUG-001/packaging-pricing
# Each shop has its own pricing based on their customs + batch
```

## Summary

Your system now provides:

1. **Global Drug Catalog** with standard packaging hierarchy
2. **Shop-Specific Batches** with FIFO management
3. **Automatic Batch Switching** when old stock exhausted
4. **Smart Price Updates** that preserve custom shop pricing
5. **Full Audit Trail** of all price changes

When a batch is sold out:

- Next batch automatically becomes active (FIFO)
- Call update endpoint to refresh prices
- System auto-calculates using: `BatchPrice × PackagingQuantity`
- Custom shop prices are preserved
- All changes are logged with formulas

🎉 **Result:** Efficient, accurate, multi-shop inventory management with automated pricing!
