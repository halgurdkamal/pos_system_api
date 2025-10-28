using System;

namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Represents a single level in the packaging hierarchy of a drug
/// Example: Base (1 tablet) -> Strip (10 tablets) -> Box (100 tablets)
/// </summary>
public class PackagingLevel
{
    /// <summary>
    /// Unique identifier for this packaging level within the drug catalog
    /// </summary>
    public string PackagingLevelId { get; set; } = $"PKG-LV-{Guid.NewGuid():N}".ToUpperInvariant();
    
    /// <summary>
    /// The level number in the hierarchy (1 = base unit, 2 = intermediate, 3+ = outer packaging)
    /// </summary>
    public int LevelNumber { get; set; }
    
    /// <summary>
    /// Name of the packaging unit at this level
    /// Examples: "Tablet", "Strip", "Box", "Bottle", "Tube", "Inhaler"
    /// </summary>
    public string UnitName { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional reference to the parent packaging level (null for base level)
    /// </summary>
    public string? ParentPackagingLevelId { get; set; }
    
    /// <summary>
    /// How many base units this packaging level contains
    /// Example: Strip contains 10 tablets -> BaseUnitQuantity = 10
    /// </summary>
    public decimal BaseUnitQuantity { get; set; }

    /// <summary>
    /// Quantity of the immediate parent unit contained within this level.
    /// Example: Box contains 10 strips -> QuantityPerParent = 10
    /// </summary>
    public decimal QuantityPerParent { get; set; } = 1;
    
    /// <summary>
    /// Whether this level can be sold to customers
    /// Example: May allow selling strips but not individual tablets
    /// </summary>
    public bool IsSellable { get; set; } = true;
    
    /// <summary>
    /// Whether this is the default sell unit for this drug
    /// Only one level should have this set to true
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// Whether this packaging level can be broken/subdivided for partial sales
    /// Example: Can break a strip to sell 5 tablets, but can't break an inhaler
    /// </summary>
    public bool IsBreakable { get; set; } = true;
    
    /// <summary>
    /// Optional barcode specific to this packaging level
    /// Example: Different barcodes for box vs strip
    /// </summary>
    public string? Barcode { get; set; }
    
    /// <summary>
    /// Minimum quantity that can be sold at this level
    /// Example: For liquids, might require minimum 5ml purchase
    /// </summary>
    public decimal? MinimumSaleQuantity { get; set; }

    public PackagingLevel() { }

    public PackagingLevel(
        string? packagingLevelId,
        int levelNumber,
        string unitName,
        decimal baseUnitQuantity,
        bool isSellable = true,
        bool isDefault = false,
        bool isBreakable = true,
        string? barcode = null,
        decimal? minimumSaleQuantity = null,
        string? parentPackagingLevelId = null,
        decimal? quantityPerParent = null)
    {
        PackagingLevelId = !string.IsNullOrWhiteSpace(packagingLevelId)
            ? packagingLevelId
            : $"PKG-LV-{Guid.NewGuid():N}".ToUpperInvariant();
        LevelNumber = levelNumber;
        UnitName = unitName;
        ParentPackagingLevelId = parentPackagingLevelId;
        BaseUnitQuantity = baseUnitQuantity;
        if (levelNumber == 1)
        {
            QuantityPerParent = 1;
            ParentPackagingLevelId = null;
        }
        else
        {
            QuantityPerParent = quantityPerParent ?? 0;
        }
        IsSellable = isSellable;
        IsDefault = isDefault;
        IsBreakable = isBreakable;
        Barcode = barcode;
        MinimumSaleQuantity = minimumSaleQuantity;
    }

    /// <summary>
    /// Calculate how many units of this level can be made from a given quantity of base units
    /// </summary>
    public decimal CalculateUnitsFromBaseQuantity(decimal baseQuantity)
    {
        if (BaseUnitQuantity <= 0) return 0;
        return baseQuantity / BaseUnitQuantity;
    }

    /// <summary>
    /// Calculate how many base units are in a given quantity of this level
    /// </summary>
    public decimal CalculateBaseUnitsFromQuantity(decimal quantity)
    {
        return quantity * BaseUnitQuantity;
    }
}
