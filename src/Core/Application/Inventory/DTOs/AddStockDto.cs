namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// DTO for adding stock to shop inventory
/// </summary>
public class AddStockDto
{
    public string DrugId { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public string SupplierId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
}
