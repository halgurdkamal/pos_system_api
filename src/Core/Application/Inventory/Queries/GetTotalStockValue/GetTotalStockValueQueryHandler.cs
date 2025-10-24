using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;

namespace pos_system_api.Core.Application.Inventory.Queries.GetTotalStockValue;

/// <summary>
/// Handler for GetTotalStockValueQuery
/// </summary>
public class GetTotalStockValueQueryHandler : IRequestHandler<GetTotalStockValueQuery, decimal>
{
    private readonly IInventoryRepository _inventoryRepository;

    public GetTotalStockValueQueryHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<decimal> Handle(GetTotalStockValueQuery request, CancellationToken cancellationToken)
    {
        return await _inventoryRepository.GetTotalStockValueAsync(request.ShopId, cancellationToken);
    }
}
