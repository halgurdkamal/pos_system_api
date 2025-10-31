namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// POS item response tailored for shop inventory listings.
/// </summary>
public class ShopPosItemDto
{
    public string InventoryId { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string? PrimaryImageUrl { get; set; }
    public bool IsAvailable { get; set; }
    public int TotalStock { get; set; }
    public int ReorderPoint { get; set; }
    public ShopPosItemPackagingDto Packaging { get; set; } = new();
    public ShopPosItemPricingDto ShopPricing { get; set; } = new();
}

public class ShopPosItemPackagingDto
{
    public string DefaultSellUnit { get; set; } = string.Empty;
    public IList<ShopPosItemPackagingLevelDto> PackagingLevels { get; set; } = new List<ShopPosItemPackagingLevelDto>();
}

public class ShopPosItemPackagingLevelDto
{
    public string UnitName { get; set; } = string.Empty;
    public bool IsDefaultSellUnit { get; set; }
    public bool IsSellable { get; set; }
    public decimal EffectiveBaseUnitQuantity { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? MinimumSaleQuantity { get; set; }
}

public class ShopPosItemPricingDto
{
    public decimal SellingPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal TaxRate { get; set; }
}
