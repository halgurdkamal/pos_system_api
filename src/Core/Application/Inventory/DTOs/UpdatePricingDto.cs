namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// DTO for updating inventory pricing
/// </summary>
public class UpdatePricingDto
{
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal Discount { get; set; } = 0;
    public decimal TaxRate { get; set; } = 0;
}
