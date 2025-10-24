using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

public interface IStockCountRepository
{
    Task<StockCount> AddAsync(StockCount stockCount, CancellationToken cancellationToken = default);
    Task<StockCount?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<StockCount> UpdateAsync(StockCount stockCount, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockCount>> GetByShopAsync(string shopId, StockCountStatus? status = null, CancellationToken cancellationToken = default);
}
