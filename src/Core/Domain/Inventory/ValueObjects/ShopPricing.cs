namespace pos_system_api.Core.Domain.Inventory.ValueObjects;

/// <summary>
/// Value object representing shop-specific pricing for a drug
/// </summary>
public class ShopPricing
{
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal Discount { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal TaxRate { get; set; }
    public DateTime LastPriceUpdate { get; set; }

    public ShopPricing() 
    {
        LastPriceUpdate = DateTime.UtcNow;
    }

    public ShopPricing(
        decimal costPrice,
        decimal sellingPrice,
        decimal discount,
        string currency,
        decimal taxRate)
    {
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        Discount = discount;
        Currency = currency;
        TaxRate = taxRate;
        LastPriceUpdate = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculate final price after discount
    /// </summary>
    public decimal GetFinalPrice()
    {
        return SellingPrice * (1 - Discount / 100);
    }

    /// <summary>
    /// Calculate price with tax
    /// </summary>
    public decimal GetPriceWithTax()
    {
        return GetFinalPrice() * (1 + TaxRate / 100);
    }

    /// <summary>
    /// Calculate profit margin percentage
    /// </summary>
    public decimal GetProfitMargin()
    {
        if (CostPrice == 0) return 0;
        return ((GetFinalPrice() - CostPrice) / CostPrice) * 100;
    }
}
