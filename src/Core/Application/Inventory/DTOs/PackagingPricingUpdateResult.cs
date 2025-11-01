using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// Result of updating packaging level prices from a batch
/// </summary>
public class PackagingPricingUpdateResult
{
    /// <summary>
    /// The updated ShopPricing object
    /// </summary>
    public ShopPricing UpdatedPricing { get; set; } = new();

    /// <summary>
    /// Summary of changes made
    /// </summary>
    public PackagingPricingUpdateSummary Summary { get; set; } = new();

    /// <summary>
    /// Whether any changes were made
    /// </summary>
    public bool HasChanges => Summary.AutoCalculatedCount > 0 || Summary.AddedCount > 0;
}

/// <summary>
/// Summary of pricing changes
/// </summary>
public class PackagingPricingUpdateSummary
{
    /// <summary>
    /// Number of packaging levels that had prices auto-calculated (were null)
    /// </summary>
    public int AutoCalculatedCount { get; set; }

    /// <summary>
    /// Number of new packaging levels added to the dictionary
    /// </summary>
    public int AddedCount { get; set; }

    /// <summary>
    /// Number of packaging levels that kept their custom prices (non-null)
    /// </summary>
    public int CustomPriceCount { get; set; }

    /// <summary>
    /// Total number of packaging levels processed
    /// </summary>
    public int TotalLevels { get; set; }

    /// <summary>
    /// Details of each change made
    /// </summary>
    public List<PackagingPriceChange> Changes { get; set; } = new();

    /// <summary>
    /// The batch used for calculations
    /// </summary>
    public string? BatchNumber { get; set; }

    /// <summary>
    /// Batch selling price used as base
    /// </summary>
    public decimal? BatchSellingPrice { get; set; }
}

/// <summary>
/// Details of a single packaging level price change
/// </summary>
public class PackagingPriceChange
{
    /// <summary>
    /// Packaging level unit name (e.g., "Box", "Strip", "Tablet")
    /// </summary>
    public string UnitName { get; set; } = string.Empty;

    /// <summary>
    /// Previous price (null if it was null before)
    /// </summary>
    public decimal? OldPrice { get; set; }

    /// <summary>
    /// New calculated price
    /// </summary>
    public decimal? NewPrice { get; set; }

    /// <summary>
    /// The effective base unit quantity used in calculation
    /// </summary>
    public decimal EffectiveBaseUnitQuantity { get; set; }

    /// <summary>
    /// Type of change
    /// </summary>
    public PriceChangeType ChangeType { get; set; }

    /// <summary>
    /// Calculation formula for transparency
    /// </summary>
    public string? CalculationFormula { get; set; }
}

/// <summary>
/// Type of price change
/// </summary>
public enum PriceChangeType
{
    /// <summary>
    /// Price was auto-calculated from null
    /// </summary>
    AutoCalculated,

    /// <summary>
    /// New packaging level was added
    /// </summary>
    Added,

    /// <summary>
    /// Custom price was kept (no change)
    /// </summary>
    CustomPriceKept,

    /// <summary>
    /// Price was re-calculated (was null, updated to new batch)
    /// </summary>
    ReCalculated
}
