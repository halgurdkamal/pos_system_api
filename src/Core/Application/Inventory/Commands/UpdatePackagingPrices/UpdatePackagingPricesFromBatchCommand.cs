using MediatR;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdatePackagingPrices;

/// <summary>
/// Command to update packaging level prices from the active batch
/// Use this when:
/// - A new batch is added to inventory
/// - An old batch is sold out and a new one becomes active
/// - You want to refresh auto-calculated prices
/// </summary>
public record UpdatePackagingPricesFromBatchCommand(
    string ShopId,
    string DrugId
) : IRequest<PackagingPricingUpdateResult>;
