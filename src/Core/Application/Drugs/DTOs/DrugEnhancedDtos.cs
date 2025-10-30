namespace pos_system_api.Core.Application.Drugs.DTOs;

/// <summary>
/// Lightweight DTO for drug list view - optimized for browsing
/// Includes: image, name, price, stock info
/// </summary>
public record DrugListItemDto
{
    public string DrugId { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string CategoryId { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? CategoryLogoUrl { get; init; }
    public string? CategoryColorCode { get; init; }
    public string? PrimaryImageUrl { get; init; } // First image only for list view
    public string Manufacturer { get; init; } = string.Empty;
    
    // Formulation basics
    public string Strength { get; init; } = string.Empty;
    public string Form { get; init; } = string.Empty;
    
    // Pricing
    public decimal SuggestedRetailPrice { get; init; }
    public decimal WholesalePrice { get; init; }
    
    // Stock summary (total across all shops)
    public int TotalQuantityInStock { get; init; }
    public int ShopCount { get; init; } // How many shops have this drug
    public bool IsAvailable { get; init; } // At least one shop has stock
    
    // Quick info
    public bool RequiresPrescription { get; init; }
}

/// <summary>
/// Detailed DTO for single drug view with full inventory across all shops
/// </summary>
public record DrugDetailDto
{
    // Basic drug info (same as DrugDto)
    public string DrugId { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string BarcodeType { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string Manufacturer { get; init; } = string.Empty;
    public string OriginCountry { get; init; } = string.Empty;
    public string CategoryId { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? CategoryLogoUrl { get; init; }
    public string? CategoryColorCode { get; init; }
    public List<string> ImageUrls { get; init; } = new();
    public string Description { get; init; } = string.Empty;
    public List<string> SideEffects { get; init; } = new();
    public List<string> InteractionNotes { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public List<string> RelatedDrugs { get; init; } = new();
    
    // Formulation details
    public FormulationDto Formulation { get; init; } = new();
    
    // Pricing details
    public PricingDto Pricing { get; init; } = new();
    
    // Regulatory details
    public RegulatoryDto Regulatory { get; init; } = new();
    
    // Shop inventory summary (NEW - this is what you requested!)
    public List<ShopInventorySummaryDto> ShopInventories { get; init; } = new();
    
    // Overall stock summary
    public int TotalQuantityInStock { get; init; }
    public int TotalShopsWithStock { get; init; }
    public decimal AveragePriceAcrossShops { get; init; }
    public decimal LowestPriceAvailable { get; init; }
    public decimal HighestPriceAvailable { get; init; }
    
    public DateTime CreatedAt { get; init; }
    public DateTime? LastUpdated { get; init; }
}

/// <summary>
/// Shop inventory summary for a specific drug
/// Shows which shops have this drug and how much
/// </summary>
public record ShopInventorySummaryDto
{
    public string ShopId { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public string? ShopAddress { get; init; }
    public int Quantity { get; init; }
    public decimal SellingPrice { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public decimal FinalPrice { get; init; }
    public int BatchCount { get; init; }
    public DateTime? NearestExpiryDate { get; init; }
    public bool IsLowStock { get; init; }
    public bool IsAvailable { get; init; }
}

/// <summary>
/// Simplified formulation DTO
/// </summary>
public record FormulationDto
{
    public string Strength { get; init; } = string.Empty;
    public string Form { get; init; } = string.Empty;
    public string RouteOfAdministration { get; init; } = string.Empty;
    public string ActiveIngredients { get; init; } = string.Empty;
}

/// <summary>
/// Simplified pricing DTO
/// </summary>
public record PricingDto
{
    public decimal SuggestedRetailPrice { get; init; }
    public decimal WholesalePrice { get; init; }
    public decimal ManufacturerPrice { get; init; }
}

/// <summary>
/// Simplified regulatory DTO
/// </summary>
public record RegulatoryDto
{
    public bool IsPrescriptionRequired { get; init; }
    public string? ControlledSubstanceSchedule { get; init; }
    public string? FdaApprovalNumber { get; init; }
}
