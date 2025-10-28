namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// Represents the merged packaging view for a shop and drug, combining global definitions with overrides.
/// </summary>
public class EffectivePackagingDto
{
    public string ShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    public IList<EffectivePackagingLevelDto> PackagingLevels { get; set; } = new List<EffectivePackagingLevelDto>();
}

public class EffectivePackagingLevelDto
{
    public string? LevelId { get; set; }
    public string? OverrideId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public bool IsGlobal { get; set; }
    public bool IsSellable { get; set; }
    public bool? GlobalIsSellable { get; set; }
    public bool IsDefaultSellUnit { get; set; }
    public decimal GlobalBaseUnitQuantity { get; set; }
    public decimal EffectiveBaseUnitQuantity { get; set; }
    public decimal GlobalQuantityPerParent { get; set; }
    public decimal EffectiveQuantityPerParent { get; set; }
    public decimal? OverrideQuantityPerParent { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? MinimumSaleQuantity { get; set; }
    public string? ParentLevelId { get; set; }
    public string? ParentOverrideId { get; set; }
    public int Sequence { get; set; }
    public bool IsCustomLevel => !IsGlobal;
}
