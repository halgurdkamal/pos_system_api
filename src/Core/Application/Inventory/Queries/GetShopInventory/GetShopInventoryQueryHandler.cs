using System.Linq;
using System.Threading.Tasks;
using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Inventory.Commands;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Services;

namespace pos_system_api.Core.Application.Inventory.Queries.GetShopInventory;

/// <summary>
/// Handler for GetShopInventoryQuery
/// </summary>
public class GetShopInventoryQueryHandler : IRequestHandler<GetShopInventoryQuery, PagedResult<InventoryDto>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IEffectivePackagingService _packagingService;

    public GetShopInventoryQueryHandler(IInventoryRepository inventoryRepository, IEffectivePackagingService packagingService)
    {
        _inventoryRepository = inventoryRepository;
        _packagingService = packagingService;
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

        var packagingTasks = inventoryDtos.Select(async dto =>
        {
            dto.Packaging = await _packagingService.GetEffectivePackagingAsync(dto.ShopId, dto.DrugId, cancellationToken);
        });

        await Task.WhenAll(packagingTasks);

        return new PagedResult<InventoryDto>(inventoryDtos, request.Page, request.Limit, totalCount);
    }
}
