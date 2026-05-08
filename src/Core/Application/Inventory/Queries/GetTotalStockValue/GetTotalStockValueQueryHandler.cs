using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;

namespace pos_system_api.Core.Application.Inventory.Queries.GetTotalStockValue;

/// <summary>
/// Handler for GetTotalStockValueQuery
/// </summary>
public class GetTotalStockValueQueryHandler : IRequestHandler<GetTotalStockValueQuery, TotalStockValueResult>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IShopRepository _shopRepository;

    public GetTotalStockValueQueryHandler(
        IInventoryRepository inventoryRepository,
        IShopRepository shopRepository)
    {
        _inventoryRepository = inventoryRepository;
        _shopRepository = shopRepository;
    }

    public async Task<TotalStockValueResult> Handle(GetTotalStockValueQuery request, CancellationToken cancellationToken)
    {
        var totalValue = await _inventoryRepository.GetTotalStockValueAsync(request.ShopId, cancellationToken);
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
        var currency = !string.IsNullOrWhiteSpace(shop?.Currency) ? shop!.Currency : "USD";
        return new TotalStockValueResult(totalValue, currency);
    }
}
