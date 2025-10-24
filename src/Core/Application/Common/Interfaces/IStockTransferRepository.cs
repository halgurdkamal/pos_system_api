using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

public interface IStockTransferRepository
{
    Task<StockTransfer> AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default);
    Task<StockTransfer?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<StockTransfer> UpdateAsync(StockTransfer transfer, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockTransfer>> GetPendingTransfersAsync(
        string? shopId = null,
        bool isSender = true,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<StockTransfer>> GetTransferHistoryAsync(
        string shopId,
        bool isSender = true,
        DateTime? startDate = null,
        DateTime? endDate = null,
        TransferStatus? status = null,
        int? limit = null,
        CancellationToken cancellationToken = default);
}
