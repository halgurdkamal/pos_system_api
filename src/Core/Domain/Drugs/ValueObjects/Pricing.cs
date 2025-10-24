namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Represents pricing information for a drug
/// </summary>
public class Pricing
{
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Discount { get; set; }
    public decimal TaxRate { get; set; }
}
