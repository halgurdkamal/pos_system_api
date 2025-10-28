using pos_system_api.Core.Domain.Common;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Core.Domain.Inventory.Entities;

/// <summary>
/// Represents shop-specific inventory for a drug (multi-tenant model)
/// Each shop maintains its own stock, batches, and pricing for drugs from the shared catalog
/// </summary>
public class ShopInventory : BaseEntity
{
    // Foreign Keys
    public string ShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    
    // Stock Information
    public int TotalStock { get; set; }
    public int ReorderPoint { get; set; } = 50;
    public string StorageLocation { get; set; } = string.Empty;
    
    // Shop-Specific Packaging Configuration
    /// <summary>
    /// Optional override for the default sell unit for this drug in this shop
    /// If null, uses the Drug's PackagingInfo default sell unit
    /// Example: Drug default is "Strip", but this shop prefers to sell by "Box"
    /// </summary>
    public string? ShopSpecificSellUnit { get; set; }
    
    /// <summary>
    /// Minimum quantity that can be sold at the shop level
    /// Overrides the packaging level minimum if specified
    /// </summary>
    public decimal? MinimumSaleQuantity { get; set; }
    
    // Batches (owned collection - configured in EF Core)
    public List<Batch> Batches { get; set; } = new();
    
    // Shop-Specific Pricing (owned entity)
    public ShopPricing ShopPricing { get; set; } = new();

    /// <summary>
    /// Shop-level overrides for packaging quantities, sellability, and pricing.
    /// </summary>
    public List<ShopPackagingOverride> PackagingOverrides { get; set; } = new();
    
    // Inventory Status
    public bool IsAvailable { get; set; } = true;
    public DateTime? LastRestockDate { get; set; }

    public ShopInventory() { }

    public ShopInventory(
        string shopId,
        string drugId,
        int reorderPoint,
        string storageLocation,
        ShopPricing shopPricing,
        string? shopSpecificSellUnit = null,
        decimal? minimumSaleQuantity = null)
    {
        Id = $"INV-{Guid.NewGuid().ToString().Substring(0, 12).ToUpper()}";
        ShopId = shopId;
        DrugId = drugId;
        TotalStock = 0;
        ReorderPoint = reorderPoint;
        StorageLocation = storageLocation;
        ShopPricing = shopPricing;
        ShopSpecificSellUnit = shopSpecificSellUnit;
        MinimumSaleQuantity = minimumSaleQuantity;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Add a new batch to inventory
    /// </summary>
    public void AddBatch(Batch batch)
    {
        Batches.Add(batch);
        RecalculateTotalStock();
        LastRestockDate = DateTime.UtcNow;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove quantity from oldest active batch (FIFO)
    /// </summary>
    public void ReduceStock(int quantity)
    {
        var remainingQuantity = quantity;
        
        // Sort batches by received date (FIFO)
        var activeBatches = Batches
            .Where(b => b.Status == BatchStatus.Active && b.QuantityOnHand > 0)
            .OrderBy(b => b.ReceivedDate)
            .ToList();

        foreach (var batch in activeBatches)
        {
            if (remainingQuantity <= 0) break;

            if (batch.QuantityOnHand >= remainingQuantity)
            {
                batch.QuantityOnHand -= remainingQuantity;
                remainingQuantity = 0;
            }
            else
            {
                remainingQuantity -= batch.QuantityOnHand;
                batch.QuantityOnHand = 0;
            }
        }

        RecalculateTotalStock();
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculate total stock from all active batches
    /// </summary>
    public void RecalculateTotalStock()
    {
        TotalStock = Batches
            .Where(b => b.Status == BatchStatus.Active)
            .Sum(b => b.QuantityOnHand);
        
        IsAvailable = TotalStock > 0;
    }

    /// <summary>
    /// Get total stock on shop floor (available for immediate sale)
    /// </summary>
    public int GetShopFloorStock()
    {
        return Batches
            .Where(b => b.Status == BatchStatus.Active && b.Location == BatchLocation.ShopFloor)
            .Sum(b => b.QuantityOnHand);
    }

    /// <summary>
    /// Get total stock in storage/warehouse
    /// </summary>
    public int GetStorageStock()
    {
        return Batches
            .Where(b => b.Status == BatchStatus.Active && b.Location == BatchLocation.Storage)
            .Sum(b => b.QuantityOnHand);
    }

    /// <summary>
    /// Get reserved stock (for transfers, orders, etc.)
    /// </summary>
    public int GetReservedStock()
    {
        return Batches
            .Where(b => b.Status == BatchStatus.Active && b.Location == BatchLocation.Reserved)
            .Sum(b => b.QuantityOnHand);
    }

    /// <summary>
    /// Get quarantined stock
    /// </summary>
    public int GetQuarantinedStock()
    {
        return Batches
            .Where(b => b.Status == BatchStatus.Active && b.Location == BatchLocation.Quarantine)
            .Sum(b => b.QuantityOnHand);
    }

    /// <summary>
    /// Move stock from storage to shop floor
    /// </summary>
    public void RestockShopFloor(int quantity, string? specificBatchNumber = null)
    {
        var remainingQuantity = quantity;
        
        IEnumerable<Batch> batchesToRestock;
        
        if (!string.IsNullOrEmpty(specificBatchNumber))
        {
            // Restock from specific batch
            batchesToRestock = Batches
                .Where(b => b.Status == BatchStatus.Active 
                    && b.Location == BatchLocation.Storage 
                    && b.BatchNumber == specificBatchNumber
                    && b.QuantityOnHand > 0)
                .OrderBy(b => b.ExpiryDate); // Use oldest expiry first (FEFO)
        }
        else
        {
            // Restock from any storage batch (FEFO - First Expired First Out)
            batchesToRestock = Batches
                .Where(b => b.Status == BatchStatus.Active 
                    && b.Location == BatchLocation.Storage 
                    && b.QuantityOnHand > 0)
                .OrderBy(b => b.ExpiryDate);
        }

        foreach (var batch in batchesToRestock)
        {
            if (remainingQuantity <= 0) break;

            var quantityToMove = Math.Min(batch.QuantityOnHand, remainingQuantity);
            
            // Create a new batch entry for shop floor or update existing
            var existingFloorBatch = Batches.FirstOrDefault(b => 
                b.BatchNumber == batch.BatchNumber && 
                b.Location == BatchLocation.ShopFloor);

            if (existingFloorBatch != null)
            {
                existingFloorBatch.QuantityOnHand += quantityToMove;
            }
            else
            {
                Batches.Add(new Batch(
                    batch.BatchNumber,
                    batch.SupplierId,
                    quantityToMove,
                    batch.ReceivedDate,
                    batch.ExpiryDate,
                    batch.PurchasePrice,
                    batch.SellingPrice,
                    BatchLocation.ShopFloor,
                    "Shop Floor"
                ));
            }

            batch.QuantityOnHand -= quantityToMove;
            remainingQuantity -= quantityToMove;
        }

        if (remainingQuantity > 0)
        {
            throw new InvalidOperationException($"Insufficient stock in storage. Requested: {quantity}, Available: {quantity - remainingQuantity}");
        }

        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Move stock from shop floor back to storage
    /// </summary>
    public void ReturnToStorage(int quantity, string? specificBatchNumber = null)
    {
        var remainingQuantity = quantity;
        
        var batchesToReturn = string.IsNullOrEmpty(specificBatchNumber)
            ? Batches.Where(b => b.Status == BatchStatus.Active 
                && b.Location == BatchLocation.ShopFloor 
                && b.QuantityOnHand > 0)
                .OrderBy(b => b.ExpiryDate)
            : Batches.Where(b => b.Status == BatchStatus.Active 
                && b.Location == BatchLocation.ShopFloor 
                && b.BatchNumber == specificBatchNumber
                && b.QuantityOnHand > 0)
                .OrderBy(b => b.ExpiryDate);

        foreach (var batch in batchesToReturn.ToList())
        {
            if (remainingQuantity <= 0) break;

            var quantityToMove = Math.Min(batch.QuantityOnHand, remainingQuantity);
            
            // Find or create storage batch
            var existingStorageBatch = Batches.FirstOrDefault(b => 
                b.BatchNumber == batch.BatchNumber && 
                b.Location == BatchLocation.Storage);

            if (existingStorageBatch != null)
            {
                existingStorageBatch.QuantityOnHand += quantityToMove;
            }
            else
            {
                Batches.Add(new Batch(
                    batch.BatchNumber,
                    batch.SupplierId,
                    quantityToMove,
                    batch.ReceivedDate,
                    batch.ExpiryDate,
                    batch.PurchasePrice,
                    batch.SellingPrice,
                    BatchLocation.Storage,
                    StorageLocation
                ));
            }

            batch.QuantityOnHand -= quantityToMove;
            remainingQuantity -= quantityToMove;
        }

        if (remainingQuantity > 0)
        {
            throw new InvalidOperationException($"Insufficient stock on shop floor. Requested: {quantity}, Available: {quantity - remainingQuantity}");
        }

        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if stock is below reorder point
    /// </summary>
    public bool IsLowStock() => TotalStock <= ReorderPoint;

    /// <summary>
    /// Get batches expiring within specified days
    /// </summary>
    public List<Batch> GetExpiringBatches(int days)
    {
        return Batches
            .Where(b => b.Status == BatchStatus.Active && b.IsExpiringWithin(days))
            .OrderBy(b => b.ExpiryDate)
            .ToList();
    }

    /// <summary>
    /// Mark expired batches
    /// </summary>
    public void MarkExpiredBatches()
    {
        foreach (var batch in Batches.Where(b => b.IsExpired() && b.Status == BatchStatus.Active))
        {
            batch.Status = BatchStatus.Expired;
        }
        
        RecalculateTotalStock();
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Update shop-specific pricing
    /// </summary>
    public void UpdatePricing(ShopPricing newPricing)
    {
        ShopPricing = newPricing;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Set or update the shop-specific default sell unit
    /// </summary>
    public void SetShopSpecificSellUnit(string? unitName)
    {
        ShopSpecificSellUnit = unitName;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Set minimum sale quantity for this shop
    /// </summary>
    public void SetMinimumSaleQuantity(decimal? quantity)
    {
        MinimumSaleQuantity = quantity;
        LastUpdated = DateTime.UtcNow;
    }
}
