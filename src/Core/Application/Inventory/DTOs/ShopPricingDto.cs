namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// DTO for ShopPricing value object
/// </summary>
public class ShopPricingDto
{
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal Discount { get; set; } = 0;
    public string Currency { get; set; } = "USD";
    public decimal TaxRate { get; set; } = 0;
    public decimal ProfitMargin { get; set; }
    public decimal ProfitMarginPercentage { get; set; }
}
