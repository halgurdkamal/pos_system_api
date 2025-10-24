using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Inventory.Commands;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Queries.GetShopInventory;

/// <summary>
/// Handler for GetShopInventoryQuery
/// </summary>
public class GetShopInventoryQueryHandler : IRequestHandler<GetShopInventoryQuery, PagedResult<InventoryDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetShopInventoryQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<PagedResult<InventoryDto>> Handle(GetShopInventoryQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _inventoryRepository.GetByShopAsync(
            request.ShopId,
            request.Page,
            request.Limit,
            request.IsAvailable,
            cancellationToken
        );

        var inventoryDtos = items.Select(InventoryMapper.MapToDto).ToList();

        return new PagedResult<InventoryDto>(inventoryDtos, request.Page, request.Limit, totalCount);
    }
}
