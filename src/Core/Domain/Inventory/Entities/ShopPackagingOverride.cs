using pos_system_api.Core.Domain.Common;

namespace pos_system_api.Core.Domain.Inventory.Entities;

/// <summary>
/// Represents a shop-specific override for packaging configuration.
/// Overrides can target existing global packaging levels or introduce custom units.
/// </summary>
public class ShopPackagingOverride : BaseEntity
{
    public string ShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the global packaging level being overridden. Null indicates a custom shop-defined level.
    /// </summary>
    public string? PackagingLevelId { get; set; }

    /// <summary>
    /// Reference to the parent global level (if override modifies quantity relative to a global parent).
    /// </summary>
    public string? ParentPackagingLevelId { get; set; }

    /// <summary>
    /// Reference to another override acting as parent (for hierarchical custom levels).
    /// </summary>
    public string? ParentOverrideId { get; set; }

    /// <summary>
    /// Optional shop-specific unit name when creating custom packaging levels.
    /// </summary>
    public string? CustomUnitName { get; set; }

    /// <summary>
    /// Overrides the quantity relative to the parent level (global or custom).
    /// </summary>
    public decimal? OverrideQuantityPerParent { get; set; }

    /// <summary>
    /// Shop-specific selling price for this level. Null falls back to default pricing logic.
    /// </summary>
    public decimal? SellingPrice { get; set; }

    /// <summary>
    /// Shop-specific sellability toggle. Null falls back to global definition.
    /// </summary>
    public bool? IsSellable { get; set; }

    /// <summary>
    /// Flags the level as the default sell unit for the shop.
    /// </summary>
    public bool? IsDefaultSellUnit { get; set; }

    /// <summary>
    /// Overrides minimum sale quantity for this shop and level.
    /// </summary>
    public decimal? MinimumSaleQuantity { get; set; }

    /// <summary>
    /// Deterministic ordering for custom levels at the same depth.
    /// </summary>
    public int? CustomLevelOrder { get; set; }

    /// <summary>
    /// Indicates whether this override represents a fully custom level.
    /// </summary>
    public bool IsCustomLevel => string.IsNullOrWhiteSpace(PackagingLevelId);
}
