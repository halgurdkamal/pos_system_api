using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

public interface IStockAdjustmentRepository
{
    Task<StockAdjustment> AddAsync(StockAdjustment adjustment, CancellationToken cancellationToken = default);
    Task<StockAdjustment?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockAdjustment>> GetByShopAsync(
        string shopId, 
        DateTime? startDate = null, 
        DateTime? endDate = null,
        AdjustmentType? adjustmentType = null,
        int? limit = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<StockAdjustment>> GetByDrugAsync(
        string shopId,
        string drugId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
    Task<int> GetTotalAdjustmentsCountAsync(
        string shopId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);
}
