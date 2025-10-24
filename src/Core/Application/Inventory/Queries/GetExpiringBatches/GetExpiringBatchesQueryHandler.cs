using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.Commands;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Queries.GetExpiringBatches;

/// <summary>
/// Handler for GetExpiringBatchesQuery
/// </summary>
public class GetExpiringBatchesQueryHandler : IRequestHandler<GetExpiringBatchesQuery, IEnumerable<InventoryDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetExpiringBatchesQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IEnumerable<InventoryDto>> Handle(GetExpiringBatchesQuery request, CancellationToken cancellationToken)
    {
        var expiringItems = await _inventoryRepository.GetExpiringBatchesAsync(
            request.ShopId,
            request.Days,
            cancellationToken
        );

        return expiringItems.Select(InventoryMapper.MapToDto).ToList();
    }
}
