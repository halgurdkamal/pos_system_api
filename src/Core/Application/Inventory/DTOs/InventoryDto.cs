namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// DTO for ShopInventory entity (API response)
/// </summary>
public class InventoryDto
{
    public string Id { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    
    // Stock Information
    public int TotalStock { get; set; }
    public int ShopFloorStock { get; set; }
    public int StorageStock { get; set; }
    public int ReservedStock { get; set; }
    public int QuarantinedStock { get; set; }
    public int ReorderPoint { get; set; }
    public string StorageLocation { get; set; } = string.Empty;
    
    // Batches
    public List<BatchDto> Batches { get; set; } = new();
    
    // Pricing
    public ShopPricingDto ShopPricing { get; set; } = new();
    
    // Status
    public bool IsAvailable { get; set; }
    public DateTime? LastRestockDate { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
