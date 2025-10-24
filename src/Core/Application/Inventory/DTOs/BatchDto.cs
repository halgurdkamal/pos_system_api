namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// DTO for Batch value object
/// </summary>
public class BatchDto
{
    public string BatchNumber { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string Status { get; set; } = "Active";
    public string Location { get; set; } = "Storage";  // Storage, ShopFloor, Reserved, Quarantine
    public string StorageLocation { get; set; } = string.Empty;  // Physical location description
}
