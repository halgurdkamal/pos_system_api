using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.Commands;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Queries.GetLowStock;

/// <summary>
/// Handler for GetLowStockQuery
/// </summary>
public class GetLowStockQueryHandler : IRequestHandler<GetLowStockQuery, IEnumerable<InventoryDto>>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetLowStockQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<IEnumerable<InventoryDto>> Handle(GetLowStockQuery request, CancellationToken cancellationToken)
    {
        var lowStockItems = await _inventoryRepository.GetLowStockAsync(request.ShopId, cancellationToken);

        return lowStockItems.Select(InventoryMapper.MapToDto).ToList();
    }
}
