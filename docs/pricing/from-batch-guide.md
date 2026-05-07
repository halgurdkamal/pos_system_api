# Automatic Packaging Price Updates from Active Batches

## Overview

This feature automatically updates packaging-level prices based on the active batch's selling price. It's designed for scenarios where:

- A shop finishes selling an old batch and a new batch becomes active
- New stock arrives with different pricing
- You want to refresh auto-calculated prices while preserving shop overrides

## How It Works

### Core Concept

When inventory batches change, the system can automatically recalculate pricing for all packaging levels (Box, Strip, Tablet, etc.) based on:

- **Active Batch Selling Price**: The per-unit price from the currently active batch (FIFO - oldest batch with stock)
- **Effective Base Unit Quantity**: How many base units are in each packaging level (calculated from packaging hierarchy)

### Pricing Logic

For each packaging level in `ShopPricing.PackagingLevelPrices`:

| Current Value   | Action                             | Reason                               |
| --------------- | ---------------------------------- | ------------------------------------ |
| `null` or `0`   | **Auto-calculate** from batch      | Price needs to be calculated         |
| Non-zero number | **Keep unchanged**                 | Shop has set a custom price override |
| Missing entry   | **Add with auto-calculated price** | New packaging level discovered       |

**Formula**: `Price = Batch Selling Price × Effective Base Unit Quantity`

## Architecture

### New Components

1. **IPackagingPricingService** - Service interface for pricing logic
2. **PackagingPricingService** - Implementation that:

   - Gets effective packaging levels using `IEffectivePackagingService`
   - Calculates prices based on batch and packaging quantities
   - Updates `ShopPricing.PackagingLevelPrices`
   - Tracks changes for audit

3. **UpdatePackagingPricesFromBatchCommand** - MediatR command for triggering updates

4. **PackagingPricingUpdateResult** - Result object containing:
   - Updated `ShopPricing`
   - Summary of changes (added, auto-calculated, custom preserved)
   - Detailed change log per packaging level

### Integration Points

- Registered in DI: `ServiceCollectionExtensions.AddApplicationLayer()`
- Exposed via API: `POST /api/inventory/shops/{shopId}/drugs/{drugId}/packaging-pricing/update-from-batch`
- Uses existing: `IEffectivePackagingService` for packaging hierarchy

## Usage Examples

### Example 1: Basic Scenario

**Setup:**

- Drug: Paracetamol 500mg
- Packaging hierarchy:
  - Box (20 strips per box)
  - Strip (10 tablets per strip)
  - Tablet (base unit)
- Old batch selling price: $0.50 per tablet
- New batch selling price: $0.60 per tablet

**Before Update:**

```json
{
  "shopPricing": {
    "costPrice": 0.4,
    "sellingPrice": 0.5,
    "packagingLevelPrices": {
      "Box": 0, // Needs calculation
      "Strip": 5.0, // Shop override - keep this
      "Tablet": 0 // Needs calculation
    }
  },
  "batches": [
    {
      "batchNumber": "BATCH-002",
      "quantityOnHand": 1000,
      "sellingPrice": 0.6,
      "status": "Active"
    }
  ]
}
```

**API Call:**

```http
POST /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing/update-from-batch
Authorization: Bearer {token}
```

**After Update:**

```json
{
  "updatedPricing": {
    "packagingLevelPrices": {
      "Box": 120.0, // Auto-calculated: 0.60 × 200 tablets
      "Strip": 5.0, // KEPT: Custom shop price
      "Tablet": 0.6 // Auto-calculated: 0.60 × 1
    },
    "lastPriceUpdate": "2025-11-01T10:30:00Z"
  },
  "summary": {
    "batchNumber": "BATCH-002",
    "batchSellingPrice": 0.6,
    "totalLevels": 3,
    "autoCalculatedCount": 2,
    "customPriceCount": 1,
    "addedCount": 0,
    "changes": [
      {
        "unitName": "Box",
        "oldPrice": 0,
        "newPrice": 120.0,
        "effectiveBaseUnitQuantity": 200,
        "changeType": "AutoCalculated",
        "calculationFormula": "0.60 (batch) × 200.00 (base units) = 120.00"
      },
      {
        "unitName": "Strip",
        "oldPrice": 5.0,
        "newPrice": 5.0,
        "effectiveBaseUnitQuantity": 10,
        "changeType": "CustomPriceKept",
        "calculationFormula": "Custom shop price retained"
      },
      {
        "unitName": "Tablet",
        "oldPrice": 0,
        "newPrice": 0.6,
        "effectiveBaseUnitQuantity": 1,
        "changeType": "AutoCalculated",
        "calculationFormula": "0.60 (batch) × 1.00 (base units) = 0.60"
      }
    ]
  }
}
```

### Example 2: Adding Missing Packaging Levels

**Setup:**

- Shop adds a new custom packaging level "Half-Box" (10 strips)
- `PackagingLevelPrices` doesn't have an entry for it yet

**Before:**

```json
{
  "packagingLevelPrices": {
    "Box": 100.0,
    "Strip": 5.0,
    "Tablet": 0.5
    // "Half-Box" is missing
  }
}
```

**After Update:**

```json
{
  "packagingLevelPrices": {
    "Box": 100.0, // Custom - kept
    "Half-Box": 50.0, // ADDED & auto-calculated: 0.50 × 100 tablets
    "Strip": 5.0, // Custom - kept
    "Tablet": 0.5 // Custom - kept
  }
}
```

**Summary:**

```json
{
  "summary": {
    "addedCount": 1,
    "autoCalculatedCount": 0,
    "customPriceCount": 3,
    "changes": [
      {
        "unitName": "Half-Box",
        "oldPrice": null,
        "newPrice": 50.0,
        "changeType": "Added"
      }
    ]
  }
}
```

### Example 3: When to Call This Endpoint

#### Scenario A: After Adding New Stock

```http
POST /api/inventory/shops/SHOP-001/drugs/DRUG-001/stock
{
  "batchNumber": "BATCH-003",
  "quantity": 500,
  "purchasePrice": 0.35,
  "sellingPrice": 0.65  // New price!
}

// Then refresh packaging prices
POST /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing/update-from-batch
```

#### Scenario B: Batch Exhaustion (Old Batch Sold Out)

```javascript
// When TotalStock reaches 0 and restocks, active batch changes
// Trigger update automatically or manually:

if (inventory.totalStock === 0) {
  await restockInventory();
  await updatePackagingPricesFromBatch(); // Prices reflect new active batch
}
```

#### Scenario C: Manual Price Refresh

```http
// Shop manager wants to reset auto-calculated prices
// after changing batch prices

POST /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing/update-from-batch
```

## Code Integration

### Using the Service Directly

```csharp
public class MyService
{
    private readonly IPackagingPricingService _pricingService;
    private readonly IInventoryRepository _inventoryRepository;

    public async Task UpdatePricesAfterRestock(string shopId, string drugId)
    {
        // Get inventory
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(shopId, drugId);

        // Get active batch (FIFO)
        var activeBatch = _pricingService.GetActiveBatch(inventory);

        if (activeBatch == null)
        {
            // No stock available
            return;
        }

        // Update prices
        var result = await _pricingService.UpdatePackagingPricesFromBatchAsync(
            inventory,
            activeBatch,
            cancellationToken);

        // Check results
        if (result.HasChanges)
        {
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

            Logger.LogInformation(
                "Updated {Count} packaging prices for {Drug} in {Shop}",
                result.Summary.AutoCalculatedCount + result.Summary.AddedCount,
                drugId,
                shopId);
        }
    }
}
```

### Using the MediatR Command

```csharp
public class OrderFulfillmentService
{
    private readonly IMediator _mediator;

    public async Task OnBatchExhausted(string shopId, string drugId)
    {
        // When old batch is sold out and new one becomes active
        var command = new UpdatePackagingPricesFromBatchCommand(shopId, drugId);
        var result = await _mediator.Send(command);

        // Log changes
        foreach (var change in result.Summary.Changes)
        {
            if (change.ChangeType == PriceChangeType.AutoCalculated)
            {
                Logger.LogInformation(
                    "Auto-calculated {Unit}: {OldPrice} → {NewPrice}",
                    change.UnitName,
                    change.OldPrice,
                    change.NewPrice);
            }
        }
    }
}
```

## API Reference

### Endpoint

```
POST /api/inventory/shops/{shopId}/drugs/{drugId}/packaging-pricing/update-from-batch
```

**Path Parameters:**

- `shopId` (string, required) - Shop identifier
- `drugId` (string, required) - Drug identifier

**Request Body:** None

**Response:** `200 OK`

```typescript
{
  updatedPricing: {
    costPrice: number,
    sellingPrice: number,
    packagingLevelPrices: { [key: string]: number },
    lastPriceUpdate: string (ISO8601)
  },
  summary: {
    batchNumber: string,
    batchSellingPrice: number,
    totalLevels: number,
    autoCalculatedCount: number,
    addedCount: number,
    customPriceCount: number,
    changes: [
      {
        unitName: string,
        oldPrice: number | null,
        newPrice: number,
        effectiveBaseUnitQuantity: number,
        changeType: "AutoCalculated" | "Added" | "CustomPriceKept",
        calculationFormula: string
      }
    ]
  }
}
```

**Error Responses:**

- `404 Not Found` - Inventory not found for shop/drug combination
- `400 Bad Request` - Invalid request parameters

## Best Practices

### 1. When to Use Auto-Calculation vs Manual Pricing

✅ **Use Auto-Calculation (set to 0 or null):**

- Standard retail pricing based on batch cost
- Consistent markup across packaging levels
- Frequent price changes due to supply chain

❌ **Use Manual Pricing (set custom value):**

- Promotional pricing ("Buy box, get discount")
- Strategic pricing (sell strips at premium)
- Market-competitive pricing
- Loyalty program special rates

### 2. Workflow Recommendations

**On Batch Arrival:**

```
1. Add batch to inventory (POST /api/inventory/.../stock)
2. Update packaging prices (POST .../update-from-batch)
3. Verify prices (GET .../packaging-pricing)
4. Notify cashiers of new pricing
```

**On Low Stock Alert:**

```
1. Check if prices need adjustment before reorder
2. Order new stock
3. When delivered, update prices from new batch
```

### 3. Price Consistency

To maintain pricing consistency:

- Set custom prices for packaging levels where you want control
- Use auto-calculation for the rest
- Review `changes` in API response to audit price updates
- Log price changes for compliance/reporting

### 4. Multi-Shop Scenarios

Each shop has independent pricing:

```json
// Shop A - Premium location
{
  "Box": 150.00,   // Custom markup
  "Strip": 0,      // Auto-calculated
  "Tablet": 0      // Auto-calculated
}

// Shop B - Budget location
{
  "Box": 0,        // Auto-calculated (competitive)
  "Strip": 0,      // Auto-calculated
  "Tablet": 0.45   // Custom discount
}
```

## Testing

### Unit Test Example

```csharp
[Fact]
public async Task UpdatePackagingPrices_AutoCalculatesNullPrices()
{
    // Arrange
    var inventory = CreateTestInventory();
    inventory.ShopPricing.PackagingLevelPrices = new Dictionary<string, decimal>
    {
        { "Box", 0 },      // Should be calculated
        { "Strip", 5.00 }, // Should be kept
        { "Tablet", 0 }    // Should be calculated
    };

    var batch = new Batch
    {
        BatchNumber = "TEST-001",
        SellingPrice = 0.60m,
        QuantityOnHand = 100,
        Status = BatchStatus.Active
    };

    // Act
    var result = await _pricingService.UpdatePackagingPricesFromBatchAsync(
        inventory, batch, CancellationToken.None);

    // Assert
    Assert.Equal(2, result.Summary.AutoCalculatedCount);
    Assert.Equal(1, result.Summary.CustomPriceCount);
    Assert.Equal(120.00m, inventory.ShopPricing.PackagingLevelPrices["Box"]);
    Assert.Equal(5.00m, inventory.ShopPricing.PackagingLevelPrices["Strip"]); // Unchanged
    Assert.Equal(0.60m, inventory.ShopPricing.PackagingLevelPrices["Tablet"]);
}
```

### Integration Test Example

```csharp
[Fact]
public async Task UpdateFromBatch_EndToEnd_Success()
{
    // Setup test data
    var shopId = "TEST-SHOP";
    var drugId = "TEST-DRUG";

    // Add inventory with batch
    await AddTestInventoryAsync(shopId, drugId);

    // Call API
    var response = await _client.PostAsync(
        $"/api/inventory/shops/{shopId}/drugs/{drugId}/packaging-pricing/update-from-batch",
        null);

    // Verify
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<PackagingPricingUpdateResult>();

    Assert.NotNull(result);
    Assert.True(result.HasChanges);
    Assert.NotEmpty(result.Summary.Changes);
}
```

## Troubleshooting

### Issue: No prices updated

**Cause:** All packaging levels have custom prices (non-zero)  
**Solution:** Set prices to 0 or null for levels you want auto-calculated

### Issue: Prices don't match expectations

**Cause:** `EffectiveBaseUnitQuantity` calculation differs from expected  
**Solution:** Check packaging overrides and hierarchy with GET /api/inventory/shops/{shopId}/drugs/{drugId}/packaging

### Issue: "No active batch" warning

**Cause:** No batches with `QuantityOnHand > 0` and `Status = Active`  
**Solution:** Ensure inventory has active stock before updating prices

## Future Enhancements

Potential improvements:

- [ ] Automatic price updates on batch exhaustion (event-driven)
- [ ] Bulk price updates for multiple drugs
- [ ] Price history tracking
- [ ] Profit margin validation (warn if markup too low)
- [ ] Integration with purchase order receiving
- [ ] Price change approval workflow for multi-user shops

## Related Documentation

- [Packaging System Guide](PACKAGING_SYSTEM_GUIDE.md)
- [Packaging Overrides Guide](PACKAGING_OVERRIDES_GUIDE.md)
- [Inventory Management API](API_TESTING_GUIDE_AUTH.md)
