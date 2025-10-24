namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Represents a batch of drugs in inventory
/// </summary>
public class Batch
{
    public string BatchNumber { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
}
