using System;
using System.Collections.Generic;
using System.Linq;

namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Contains complete packaging information for a drug
/// Defines how the drug is measured, packaged, and sold
/// </summary>
public class PackagingInfo
{
    /// <summary>
    /// The type of unit measurement system used for this drug
    /// </summary>
    public UnitType UnitType { get; set; }

    /// <summary>
    /// The base unit symbol used for measurement
    /// Examples: "ml", "tablet", "g", "puff"
    /// </summary>
    public string BaseUnit { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the base unit
    /// Examples: "Milliliter", "Tablet", "Gram", "Puff"
    /// </summary>
    public string BaseUnitDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the base unit can be subdivided
    /// Example: Liquids (true), Inhalers (false)
    /// </summary>
    public bool IsSubdivisible { get; set; } = true;

    /// <summary>
    /// All packaging levels for this drug (ordered by level number)
    /// Must include at least the base level (LevelNumber = 1)
    /// </summary>
    public List<PackagingLevel> PackagingLevels { get; set; } = new();

    public PackagingInfo() { }

    public PackagingInfo(
        UnitType unitType,
        string baseUnit,
        string baseUnitDisplayName,
        bool isSubdivisible = true)
    {
        UnitType = unitType;
        BaseUnit = baseUnit;
        BaseUnitDisplayName = baseUnitDisplayName;
        IsSubdivisible = isSubdivisible;
        PackagingLevels = new List<PackagingLevel>();
    }

    /// <summary>
    /// Get the default sell unit (the packaging level marked as default)
    /// </summary>
    public PackagingLevel? GetDefaultSellUnit()
    {
        return PackagingLevels.FirstOrDefault(l => l.IsDefault);
    }

    /// <summary>
    /// Get all sellable packaging levels
    /// </summary>
    public List<PackagingLevel> GetSellableLevels()
    {
        return PackagingLevels
            .Where(l => l.IsSellable)
            .OrderBy(l => l.LevelNumber)
            .ToList();
    }

    /// <summary>
    /// Get a specific packaging level by unit name
    /// </summary>
    public PackagingLevel? GetLevelByUnitName(string unitName)
    {
        return PackagingLevels
            .FirstOrDefault(l => l.UnitName.Equals(unitName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get a packaging level by its identifier
    /// </summary>
    public PackagingLevel? GetLevelById(string packagingLevelId)
    {
        return PackagingLevels
            .FirstOrDefault(l => l.PackagingLevelId.Equals(packagingLevelId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get the base level (LevelNumber = 1)
    /// </summary>
    public PackagingLevel? GetBaseLevel()
    {
        return PackagingLevels.FirstOrDefault(l => l.LevelNumber == 1);
    }

    /// <summary>
    /// Add a packaging level to the hierarchy
    /// </summary>
    public void AddPackagingLevel(PackagingLevel level)
    {
        // If this is being set as default, unset any existing default
        if (level.IsDefault)
        {
            foreach (var existingLevel in PackagingLevels)
            {
                existingLevel.IsDefault = false;
            }
        }

        // Ensure an identifier is present
        if (string.IsNullOrWhiteSpace(level.PackagingLevelId))
        {
            level.PackagingLevelId = $"PKG-LV-{Guid.NewGuid():N}".ToUpperInvariant();
        }

        // Determine parent reference if applicable
        PackagingLevel? parentLevel = null;
        if (level.LevelNumber > 1)
        {
            if (!string.IsNullOrWhiteSpace(level.ParentPackagingLevelId))
            {
                parentLevel = PackagingLevels.FirstOrDefault(l =>
                    l.PackagingLevelId.Equals(level.ParentPackagingLevelId, StringComparison.OrdinalIgnoreCase));
            }

            parentLevel ??= PackagingLevels.FirstOrDefault(l => l.LevelNumber == level.LevelNumber - 1);

            if (parentLevel != null)
            {
                level.ParentPackagingLevelId = parentLevel.PackagingLevelId;
                if (level.QuantityPerParent <= 0 && parentLevel.BaseUnitQuantity > 0)
                {
                    level.QuantityPerParent = level.BaseUnitQuantity / parentLevel.BaseUnitQuantity;
                }
            }
        }

        PackagingLevels.Add(level);
        PackagingLevels = PackagingLevels.OrderBy(l => l.LevelNumber).ToList();
    }

    /// <summary>
    /// Validate the packaging configuration
    /// </summary>
    public (bool IsValid, List<string> Errors) Validate()
    {
        var errors = new List<string>();

        // Must have at least one level
        if (!PackagingLevels.Any())
        {
            errors.Add("PackagingInfo must have at least one packaging level");
        }

        // Must have a base level (LevelNumber = 1)
        if (!PackagingLevels.Any(l => l.LevelNumber == 1))
        {
            errors.Add("PackagingInfo must have a base level (LevelNumber = 1)");
        }

        // Ensure level ids are unique
        if (PackagingLevels.GroupBy(l => l.PackagingLevelId, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
        {
            errors.Add("Each packaging level must have a unique PackagingLevelId");
        }

        // Only one level can be default
        var defaultCount = PackagingLevels.Count(l => l.IsDefault);
        if (defaultCount > 1)
        {
            errors.Add("Only one packaging level can be marked as default");
        }

        // Default level must be sellable
        var defaultLevel = GetDefaultSellUnit();
        if (defaultLevel != null && !defaultLevel.IsSellable)
        {
            errors.Add("Default sell unit must be sellable");
        }

        // Level numbers should be sequential
        var levelNumbers = PackagingLevels.Select(l => l.LevelNumber).OrderBy(n => n).ToList();
        for (int i = 0; i < levelNumbers.Count; i++)
        {
            if (levelNumbers[i] != i + 1)
            {
                errors.Add($"Packaging level numbers must be sequential starting from 1");
                break;
            }
        }

        // Base unit quantity validations
        foreach (var level in PackagingLevels)
        {
            if (level.BaseUnitQuantity <= 0)
            {
                errors.Add($"Level {level.LevelNumber} ({level.UnitName}) must have BaseUnitQuantity > 0");
            }

            if (level.LevelNumber > 1 && level.QuantityPerParent <= 0)
            {
                errors.Add($"Level {level.LevelNumber} ({level.UnitName}) must have QuantityPerParent > 0");
            }

            if (level.LevelNumber > 1 && string.IsNullOrWhiteSpace(level.ParentPackagingLevelId))
            {
                errors.Add($"Level {level.LevelNumber} ({level.UnitName}) must reference a parent packaging level");
            }
        }

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Convert a quantity from one packaging level to another
    /// </summary>
    public decimal ConvertQuantity(decimal quantity, string fromUnit, string toUnit)
    {
        var fromLevel = GetLevelByUnitName(fromUnit);
        var toLevel = GetLevelByUnitName(toUnit);

        if (fromLevel == null || toLevel == null)
        {
            throw new ArgumentException("Invalid unit names provided");
        }

        // Convert to base units first, then to target unit
        var baseQuantity = fromLevel.CalculateBaseUnitsFromQuantity(quantity);
        return toLevel.CalculateUnitsFromBaseQuantity(baseQuantity);
    }
}
