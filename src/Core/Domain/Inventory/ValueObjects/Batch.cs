namespace pos_system_api.Core.Domain.Inventory.ValueObjects;

/// <summary>
/// Value object representing a batch of drugs in shop inventory
/// </summary>
public class Batch
{
    public string BatchNumber { get; set; } = string.Empty;
    public string? SupplierId { get; set; }
    public int QuantityOnHand { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public BatchStatus Status { get; set; } = BatchStatus.Active;
    
    // Location Tracking
    public BatchLocation Location { get; set; } = BatchLocation.Storage;
    public string StorageLocation { get; set; } = string.Empty; // e.g., "Shelf A-3", "Storage Room 2", "Counter Display"

    public Batch() { }

    public Batch(
        string batchNumber,
        string? supplierId,
        int quantityOnHand,
        DateTime receivedDate,
        DateTime expiryDate,
        decimal purchasePrice,
        decimal sellingPrice,
        BatchLocation location = BatchLocation.Storage,
        string storageLocation = "")
    {
        BatchNumber = batchNumber;
        SupplierId = supplierId;
        QuantityOnHand = quantityOnHand;
        ReceivedDate = receivedDate;
        ExpiryDate = expiryDate;
        PurchasePrice = purchasePrice;
        SellingPrice = sellingPrice;
        Status = BatchStatus.Active;
        Location = location;
        StorageLocation = storageLocation;
    }

    /// <summary>
    /// Check if batch is expired
    /// </summary>
    public bool IsExpired() => DateTime.UtcNow > ExpiryDate;

    /// <summary>
    /// Check if batch is expiring within specified days
    /// </summary>
    public bool IsExpiringWithin(int days) => 
        (ExpiryDate - DateTime.UtcNow).TotalDays <= days && !IsExpired();
}

/// <summary>
/// Physical location of batch in shop
/// </summary>
public enum BatchLocation
{
    /// <summary>Back storage/warehouse - not accessible to customers</summary>
    Storage = 0,
    
    /// <summary>Shop floor/display - accessible to customers for immediate sale</summary>
    ShopFloor = 1,
    
    /// <summary>Reserved for specific purpose (transfer, order, etc.)</summary>
    Reserved = 2,
    
    /// <summary>Quarantined - quality issue or recall</summary>
    Quarantine = 3
}

/// <summary>
/// Status of a batch in inventory
/// </summary>
public enum BatchStatus
{
    Active = 0,
    Expired = 1,
    Recalled = 2,
    Reserved = 3
}
