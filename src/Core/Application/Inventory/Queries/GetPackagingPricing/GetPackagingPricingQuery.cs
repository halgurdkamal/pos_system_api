using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Queries.GetPackagingPricing;

/// <summary>
/// Returns the shop-specific pricing for an inventory item, including all
/// configured packaging-level prices. Returns null when no inventory record
/// exists for the shop+drug pair.
/// </summary>
public record GetPackagingPricingQuery(string ShopId, string DrugId)
    : IRequest<ShopPricingDto?>;

public class GetPackagingPricingQueryHandler
    : IRequestHandler<GetPackagingPricingQuery, ShopPricingDto?>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetPackagingPricingQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<ShopPricingDto?> Handle(
        GetPackagingPricingQuery request,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(
            request.ShopId, request.DrugId, cancellationToken);

        if (inventory == null)
        {
            return null;
        }

        var pricing = inventory.ShopPricing;
        var finalPrice = pricing.GetFinalPrice();

        return new ShopPricingDto
        {
            CostPrice = pricing.CostPrice,
            SellingPrice = pricing.SellingPrice,
            Discount = pricing.Discount,
            Currency = pricing.Currency,
            TaxRate = pricing.TaxRate,
            ProfitMargin = finalPrice - pricing.CostPrice,
            ProfitMarginPercentage = pricing.GetProfitMargin(),
            LastPriceUpdate = pricing.LastPriceUpdate,
            PackagingLevelPrices = new Dictionary<string, decimal>(pricing.PackagingLevelPrices),
        };
    }
}
