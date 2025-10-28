namespace pos_system_api.Core.Application.Inventory.DTOs;

public class PackagingOverrideInputDto
{
    public string? PackagingLevelId { get; set; }
    public string? ParentPackagingLevelId { get; set; }
    public string? ParentOverrideId { get; set; }
    public string? CustomUnitName { get; set; }
    public decimal? OverrideQuantityPerParent { get; set; }
    public decimal? SellingPrice { get; set; }
    public bool? IsSellable { get; set; }
    public bool? IsDefaultSellUnit { get; set; }
    public decimal? MinimumSaleQuantity { get; set; }
    public int? CustomLevelOrder { get; set; }
}
