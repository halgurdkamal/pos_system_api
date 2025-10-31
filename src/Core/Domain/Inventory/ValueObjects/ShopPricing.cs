using System.Collections.Generic;

namespace pos_system_api.Core.Domain.Inventory.ValueObjects;

/// <summary>
/// Value object representing shop-specific pricing for a drug
/// </summary>
public class ShopPricing
{
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; } // Default selling price (for backward compatibility)
    public decimal Discount { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal TaxRate { get; set; }
    public DateTime LastPriceUpdate { get; set; }

    /// <summary>
    /// Shop-specific pricing for different packaging levels
    /// Key: Packaging level name (e.g., "Box", "Strip", "Tablet")
    /// Value: Selling price for that packaging level
    /// </summary>
    public Dictionary<string, decimal> PackagingLevelPrices { get; set; } = new();

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
        PackagingLevelPrices = new Dictionary<string, decimal>();
    }

    /// <summary>
    /// Get selling price for a specific packaging level
    /// Falls back to default SellingPrice if packaging level not found
    /// </summary>
    public decimal GetPackagingLevelPrice(string packagingLevel)
    {
        if (PackagingLevelPrices.TryGetValue(packagingLevel, out var price))
        {
            return price;
        }
        return SellingPrice; // Fallback to default price
    }

    /// <summary>
    /// Set selling price for a specific packaging level
    /// </summary>
    public void SetPackagingLevelPrice(string packagingLevel, decimal price)
    {
        PackagingLevelPrices[packagingLevel] = price;
        PackagingLevelPrices = new Dictionary<string, decimal>(PackagingLevelPrices);
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
    /// Calculate final price for a specific packaging level after discount
    /// </summary>
    public decimal GetFinalPackagingLevelPrice(string packagingLevel)
    {
        var basePrice = GetPackagingLevelPrice(packagingLevel);
        return basePrice * (1 - Discount / 100);
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
