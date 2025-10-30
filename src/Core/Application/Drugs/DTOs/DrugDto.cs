using pos_system_api.Core.Domain.Drugs.ValueObjects;

namespace pos_system_api.Core.Application.Drugs.DTOs;

/// <summary>
/// Data Transfer Object for Drug entity
/// </summary>
public class DrugDto
{
    public string DrugId { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeType { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public List<string> SideEffects { get; set; } = new();
    public List<string> InteractionNotes { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> RelatedDrugs { get; set; } = new();
    
    // Value Objects (shared catalog data only)
    public Formulation Formulation { get; set; } = new();
    public BasePricing BasePricing { get; set; } = new();
    public Regulatory Regulatory { get; set; } = new();
    
    // NOTE: Inventory, shop-specific pricing, and supplier info are now in ShopInventory (per-shop)
    
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public string? UpdatedBy { get; set; }
}
