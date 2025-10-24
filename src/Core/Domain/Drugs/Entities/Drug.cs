using pos_system_api.Core.Domain.Common;
using pos_system_api.Core.Domain.Drugs.ValueObjects;

namespace pos_system_api.Core.Domain.Drugs.Entities;

/// <summary>
/// Drug entity representing a pharmaceutical product in the POS system
/// </summary>
public class Drug : BaseEntity
{
    public string DrugId { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeType { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public List<string> SideEffects { get; set; } = new();
    public List<string> InteractionNotes { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> RelatedDrugs { get; set; } = new();
    
    // Value Objects (shared catalog data - no shop-specific info)
    public Formulation Formulation { get; set; } = new();
    public BasePricing BasePricing { get; set; } = new(); // Suggested pricing only
    public Regulatory Regulatory { get; set; } = new();
    
    // NOTE: Inventory, shop-specific pricing, and supplier info are now in ShopInventory entity
}
