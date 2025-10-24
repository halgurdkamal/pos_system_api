using MediatR;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Queries.GetShopInventory;

/// <summary>
/// Query to get paginated inventory for a shop
/// </summary>
public record GetShopInventoryQuery(
    string ShopId,
    int Page = 1,
    int Limit = 20,
    bool? IsAvailable = null
) : IRequest<PagedResult<InventoryDto>>;
