namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Base pricing information for a drug (suggested retail price)
/// Individual shops can override this in ShopInventory
/// </summary>
public class BasePricing
{
    public decimal SuggestedRetailPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal SuggestedTaxRate { get; set; }
    public DateTime? LastPriceUpdate { get; set; }

    public BasePricing() { }

    public BasePricing(
        decimal suggestedRetailPrice,
        string currency,
        decimal suggestedTaxRate)
    {
        SuggestedRetailPrice = suggestedRetailPrice;
        Currency = currency;
        SuggestedTaxRate = suggestedTaxRate;
        LastPriceUpdate = DateTime.UtcNow;
    }
}
