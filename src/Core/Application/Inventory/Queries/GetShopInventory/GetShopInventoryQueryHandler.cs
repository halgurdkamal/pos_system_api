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

    public GetShopInventoryQueryHandler(
        IInventoryRepository inventoryRepository,
        IEffectivePackagingService packagingService)
    {
        _inventoryRepository = inventoryRepository;
        _packagingService = packagingService;
    }

    public async Task<PagedResult<InventoryDto>> Handle(
        GetShopInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _inventoryRepository.GetByShopAsync(
            request.ShopId,
            request.Page,
            request.Limit,
            request.IsAvailable,
            cancellationToken);

        var inventoryDtos = items.Select(InventoryMapper.MapToDto).ToList();

        // Resolve effective packaging for the whole page in a single batch — a constant
        // number of database round-trips regardless of page size, instead of the previous
        // 3 * pageSize queries fanned out via Task.WhenAll (which also conflicted on the
        // shared DbContext).
        var drugIds = inventoryDtos.Select(d => d.DrugId).Distinct().ToList();
        if (drugIds.Count > 0)
        {
            var packagingByDrugId = await _packagingService.GetEffectivePackagingBatchAsync(
                request.ShopId, drugIds, cancellationToken);

            foreach (var dto in inventoryDtos)
            {
                if (packagingByDrugId.TryGetValue(dto.DrugId, out var packaging))
                {
                    dto.Packaging = packaging;
                }
            }
        }

        return new PagedResult<InventoryDto>(inventoryDtos, request.Page, request.Limit, totalCount);
    }
}
