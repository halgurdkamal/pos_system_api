using MediatR;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdatePackagingPricing;

/// <summary>
/// Command to update shop-specific packaging-level pricing for an inventory item
/// </summary>
public record UpdatePackagingPricingCommand(
    string ShopId,
    string DrugId,
    Dictionary<string, decimal> PackagingLevelPrices
) : IRequest<InventoryDto>;