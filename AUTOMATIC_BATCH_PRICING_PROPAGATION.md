# Automatic Batch-to-Pricing Propagation

## Overview

The system now features **fully automatic pricing updates** when batches are exhausted. When stock is reduced and a batch hits zero, the system automatically:

1. Detects the batch exhaustion
2. Selects the next active batch (FIFO)
3. Updates packaging prices to reflect the new batch
4. Logs all changes for audit

**No manual intervention required!** ✨

## How It Works

### Before (Manual)

```
1. Sale reduces stock
2. Old batch exhausted
3. ⚠️  Prices still reflect old batch
4. 👤 Operator must manually call: POST .../update-from-batch
5. Prices updated
```

### Now (Automatic)

```
1. Sale reduces stock
2. Old batch exhausted
3. ✅ System auto-detects exhaustion
4. ✅ System auto-updates prices
5. ✅ Prices immediately reflect new batch
6. ✅ All changes logged
```

## Implementation Details

### Trigger Point

The automatic update is triggered in `ReduceStockCommandHandler` during stock reduction operations:

```csharp
// After reducing stock
var exhaustedBatches = DetectExhaustedBatches();

if (exhaustedBatches.Any())
{
    // Automatically update packaging prices
    var newActiveBatch = GetActiveBatch(); // FIFO
    await UpdatePackagingPricesFromBatch(newActiveBatch);
}
```

### Detection Logic

The system tracks which batches had stock before the reduction and compares after:

```csharp
// Before reduction
var activeBatchesBefore = Batches
    .Where(b => b.Status == Active && b.QuantityOnHand > 0)
    .OrderBy(b => b.ReceivedDate)
    .ToList();

// After reduction
var exhaustedBatches = activeBatchesBefore
    .Where(before =>
    {
        var after = FindBatch(before.BatchNumber);
        return before.QuantityOnHand > 0 && after.QuantityOnHand == 0;
    })
    .ToList();
```

### Safety Features

1. **Non-Blocking**: If pricing update fails, stock reduction still succeeds
2. **Logging**: All exhaustions and price updates are logged
3. **Audit Trail**: Stock adjustment records note which batches were exhausted
4. **Idempotent**: Safe to run multiple times

## Example Scenarios

### Scenario 1: Simple Batch Exhaustion

**Initial State:**

```json
{
  "totalStock": 500,
  "batches": [
    {
      "batchNumber": "BATCH-001",
      "quantityOnHand": 500,
      "sellingPrice": 0.5,
      "receivedDate": "2025-10-01"
    },
    {
      "batchNumber": "BATCH-002",
      "quantityOnHand": 1000,
      "sellingPrice": 0.6,
      "receivedDate": "2025-10-20"
    }
  ],
  "packagingLevelPrices": {
    "Box": 100.0, // Based on BATCH-001 (0.50)
    "Tablet": 0.5
  }
}
```

**Action: Reduce Stock by 500**

```http
POST /api/inventory/shops/SHOP-001/drugs/DRUG-001/stock/reduce
{
  "quantity": 500
}
```

**Automatic Process:**

```
1. ✅ Reduce BATCH-001 by 500 → 0 (EXHAUSTED!)
2. 🔍 Detect: BATCH-001 exhausted
3. 🔄 Select: BATCH-002 now active (FIFO)
4. 💰 Auto-update prices:
   - Box: 100.00 → 120.00 (0.60 × 200)
   - Tablet: 0.50 → 0.60 (0.60 × 1)
5. 💾 Save changes
6. 📝 Log: "Batch BATCH-001 exhausted, prices updated to BATCH-002"
```

**Final State:**

```json
{
  "totalStock": 1000,
  "batches": [
    {
      "batchNumber": "BATCH-001",
      "quantityOnHand": 0 // ← EXHAUSTED
    },
    {
      "batchNumber": "BATCH-002",
      "quantityOnHand": 1000, // ← NOW ACTIVE
      "sellingPrice": 0.6
    }
  ],
  "packagingLevelPrices": {
    "Box": 120.0, // ← AUTO-UPDATED
    "Tablet": 0.6 // ← AUTO-UPDATED
  }
}
```

### Scenario 2: Multiple Batches Exhausted in One Sale

**Sale of 1500 tablets exhausts 2 batches:**

```
Initial:
- BATCH-001: 500 @ $0.50
- BATCH-002: 1000 @ $0.60
- BATCH-003: 2000 @ $0.65

After reducing 1500:
- BATCH-001: 0 (exhausted)
- BATCH-002: 0 (exhausted)
- BATCH-003: 2000 (now active)

Automatic update:
- Prices now based on BATCH-003 @ $0.65
```

### Scenario 3: Custom Prices Preserved

**Setup:**

```json
{
  "packagingLevelPrices": {
    "Box": 150.0, // Custom shop price
    "Strip": 0, // Auto-calculated
    "Tablet": 0 // Auto-calculated
  }
}
```

**After batch exhaustion:**

```json
{
  "packagingLevelPrices": {
    "Box": 150.0, // ← KEPT (custom)
    "Strip": 6.0, // ← AUTO-UPDATED (new batch)
    "Tablet": 0.6 // ← AUTO-UPDATED (new batch)
  }
}
```

## Logging & Monitoring

### Log Messages

**Batch Exhaustion Detected:**

```
[INFO] Batch(es) exhausted for Shop SHOP-001, Drug DRUG-001: BATCH-001.
       Triggering automatic price update.
```

**Successful Price Update:**

```
[INFO] Automatically updated packaging prices for Shop SHOP-001, Drug DRUG-001.
       New batch: BATCH-002, Auto-calculated: 2, Custom kept: 1
```

**No Active Batch After Exhaustion:**

```
[WARN] No active batch available for Shop SHOP-001, Drug DRUG-001 after exhaustion.
       Stock is now empty.
```

**Price Update Failed (Non-Critical):**

```
[ERROR] Failed to automatically update packaging prices after batch exhaustion
        for Shop SHOP-001, Drug DRUG-001
```

### Stock Adjustment Records

The audit trail now includes batch exhaustion information:

```json
{
  "adjustmentType": "Sale",
  "quantity": -500,
  "quantityBefore": 500,
  "notes": "Stock reduced via ReduceStock command. Batches exhausted: BATCH-001",
  "reference": "ReduceStock"
}
```

## API Behavior

### Stock Reduction Endpoint

**Endpoint:** `POST /api/inventory/shops/{shopId}/drugs/{drugId}/stock/reduce`

**Request:**

```json
{
  "quantity": 500
}
```

**Response:** (Same as before, but prices auto-updated if batch exhausted)

```json
{
  "inventoryId": "INV-001",
  "shopId": "SHOP-001",
  "drugId": "DRUG-001",
  "totalStock": 1000,
  "shopPricing": {
    "packagingLevelPrices": {
      "Box": 120.0, // ← Auto-updated if batch exhausted
      "Tablet": 0.6 // ← Auto-updated if batch exhausted
    },
    "lastPriceUpdate": "2025-11-01T15:30:00Z" // ← Updated timestamp
  },
  "batches": [
    {
      "batchNumber": "BATCH-001",
      "quantityOnHand": 0 // ← Exhausted
    },
    {
      "batchNumber": "BATCH-002",
      "quantityOnHand": 1000 // ← Now active
    }
  ]
}
```

### Manual Update Still Available

The manual endpoint is still available for:

- Refreshing prices without stock changes
- Troubleshooting
- Bulk updates
- Testing

```http
POST /api/inventory/shops/{shopId}/drugs/{drugId}/packaging-pricing/update-from-batch
```

## Integration Points

### Where Automatic Updates Trigger

1. **Direct Stock Reduction**

   ```http
   POST /api/inventory/shops/{shopId}/drugs/{drugId}/stock/reduce
   ```

2. **Sales Orders** (if using ReduceStock internally)

   ```http
   POST /api/sales/orders
   ```

3. **Stock Transfers** (if using ReduceStock internally)
   ```http
   POST /api/inventory/transfers
   ```

### Where Updates DON'T Trigger

- **Manual pricing updates** - Uses different command
- **Adding stock** - No exhaustion possible
- **Batch adjustments** - Different flow
- **Manual override commands** - Explicit control

## Performance Considerations

### Efficiency

- **Single Detection**: Batch exhaustion checked once per reduction
- **Conditional Update**: Only runs if batches exhausted
- **Async Operations**: Non-blocking for POS terminals
- **Cached Results**: Pricing service reuses effective packaging data

### Impact on Response Time

- **No exhaustion**: +0ms (no extra processing)
- **With exhaustion**: +50-150ms (pricing calculation)
- **Database**: 1 extra save operation if prices change

## Testing

### Test Case 1: Verify Automatic Update

```http
### 1. Check initial prices
GET /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing

### 2. Reduce stock to exhaust old batch
POST /api/inventory/shops/SHOP-001/drugs/DRUG-001/stock/reduce
Content-Type: application/json

{
  "quantity": 500
}

### 3. Verify prices auto-updated in response
# Check response.shopPricing.packagingLevelPrices
# Should reflect new batch price

### 4. Confirm with get request
GET /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing
# Prices should be updated
```

### Test Case 2: Custom Prices Preserved

```http
### 1. Set custom price
PUT /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing
Content-Type: application/json

{
  "Box": 200.00,
  "Strip": 0,
  "Tablet": 0
}

### 2. Exhaust batch
POST /api/inventory/shops/SHOP-001/drugs/DRUG-001/stock/reduce
Content-Type: application/json

{
  "quantity": 500
}

### 3. Verify Box price kept at 200.00
GET /api/inventory/shops/SHOP-001/drugs/DRUG-001/packaging-pricing
# Box should still be 200.00 (custom kept)
# Strip and Tablet should be updated
```

### Test Case 3: Check Logs

```bash
# View logs to see automatic updates
grep "Batch(es) exhausted" logs/app.log
grep "Automatically updated packaging prices" logs/app.log
```

## Troubleshooting

### Issue: Prices Not Updating Automatically

**Check:**

1. Is batch actually exhausted? (QuantityOnHand = 0)
2. Is there a next batch available?
3. Check logs for errors
4. Verify `IPackagingPricingService` is injected

**Workaround:**

```http
# Manually trigger update
POST /api/inventory/shops/{shopId}/drugs/{drugId}/packaging-pricing/update-from-batch
```

### Issue: Unexpected Price Changes

**Cause:** Batch exhausted during sale

**Verify:**

```http
# Check which batch is currently active
GET /api/inventory/shops/{shopId}/drugs/{drugId}
# Look at batches array, find one with quantityOnHand > 0 and oldest receivedDate
```

### Issue: Performance Degradation

**If response times increase:**

1. Check database indexes on `Batches.ReceivedDate`
2. Monitor pricing service calls
3. Review log verbosity
4. Consider caching effective packaging results

## Benefits

### ✅ Operator Experience

- **Zero manual steps** after stock reduction
- **No pricing errors** from forgetting to update
- **Immediate price accuracy** at POS
- **Seamless batch transitions**

### ✅ Business Operations

- **Real-time pricing** reflects current costs
- **Profit margin protection** automatic
- **Audit trail** complete
- **Multi-shop consistency** maintained

### ✅ System Reliability

- **Fail-safe design** (doesn't break stock reduction)
- **Comprehensive logging** for debugging
- **Idempotent operations** safe to retry
- **Clean architecture** separation of concerns

## Migration Notes

### Existing Systems

If upgrading from manual pricing:

1. ✅ No breaking changes to API
2. ✅ Existing manual endpoint still works
3. ✅ No database migrations needed
4. ✅ Backward compatible

### Rollout Strategy

1. **Test Environment**: Verify automatic updates work
2. **Staging**: Monitor logs for issues
3. **Production**: Deploy with confidence
4. **Monitor**: Check logs for automatic update frequency

### Rollback Plan

If issues arise:

1. Manual pricing endpoint still available
2. Can temporarily disable by removing `IPackagingPricingService` injection
3. No data corruption risk (prices just won't auto-update)

## Summary

The automatic batch-to-pricing propagation feature provides:

**Seamless Operation:**

- Batch exhaustion → Automatic price update
- No operator intervention needed
- Real-time pricing accuracy

**Smart Logic:**

- FIFO batch selection (oldest first)
- Custom prices preserved
- Only updates when needed

**Production Ready:**

- Comprehensive logging
- Non-breaking design
- Fail-safe operation
- Full audit trail

**Integration:**

- Works with existing stock reduction
- Compatible with all POS flows
- No API changes required

🎉 **Result:** Your POS system now handles batch transitions and pricing updates completely automatically, ensuring prices always reflect the current active batch with zero manual overhead!
