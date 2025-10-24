using MediatR;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdatePricing;

/// <summary>
/// Command to update shop-specific pricing for an inventory item
/// </summary>
public record UpdatePricingCommand(
    string ShopId,
    string DrugId,
    decimal CostPrice,
    decimal SellingPrice,
    decimal? TaxRate = null
) : IRequest<InventoryDto>;
