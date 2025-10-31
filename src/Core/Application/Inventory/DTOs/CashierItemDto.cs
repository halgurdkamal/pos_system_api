namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// Cashier-specific DTO optimized for POS terminal use
/// Combines drug info, inventory, and pricing in one model
/// </summary>
public record CashierItemDto
{
    // Drug information
    public string DrugId { get; init; } = string.Empty;
    public string BrandName { get; init; } = string.Empty;
    public string GenericName { get; init; } = string.Empty;
    public string Barcode { get; init; } = string.Empty;
    public string CategoryId { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? CategoryLogoUrl { get; init; } // NEW: Category logo for UI
    public string? CategoryColorCode { get; init; } // NEW: Category color for UI theming
    public string Manufacturer { get; init; } = string.Empty;
    public List<string> ImageUrls { get; init; } = new();

    // Stock information
    public int AvailableStock { get; init; }
    public bool IsAvailable { get; init; }
    public string? OldestBatchNumber { get; init; }
    public DateTime? NearestExpiryDate { get; init; }

    // Pricing information
    public decimal UnitPrice { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public decimal FinalPrice { get; init; }

    // Formulation information
    public string Strength { get; init; } = string.Empty;
    public string Form { get; init; } = string.Empty;
    public string PackageSize { get; init; } = string.Empty;

    // Quick info for cashier
    public bool RequiresPrescription { get; init; }
    public string? QuickNotes { get; init; }
}
