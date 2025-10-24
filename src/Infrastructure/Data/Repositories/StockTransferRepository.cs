using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.Infrastructure.Data.Repositories;

public class StockTransferRepository : IStockTransferRepository
{
    private readonly ApplicationDbContext _context;

    public StockTransferRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockTransfer> AddAsync(StockTransfer transfer, CancellationToken cancellationToken = default)
    {
        await _context.StockTransfers.AddAsync(transfer, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return transfer;
    }

    public async Task<StockTransfer?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.StockTransfers
            .FirstOrDefaultAsync(st => st.Id == id, cancellationToken);
    }

    public async Task<StockTransfer> UpdateAsync(StockTransfer transfer, CancellationToken cancellationToken = default)
    {
        _context.StockTransfers.Update(transfer);
        await _context.SaveChangesAsync(cancellationToken);
        return transfer;
    }

    public async Task<IEnumerable<StockTransfer>> GetPendingTransfersAsync(
        string? shopId = null,
        bool isSender = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockTransfers
            .AsNoTracking()
            .Where(st => st.Status == TransferStatus.Pending || st.Status == TransferStatus.Approved || st.Status == TransferStatus.InTransit);

        if (!string.IsNullOrEmpty(shopId))
        {
            query = isSender
                ? query.Where(st => st.FromShopId == shopId)
                : query.Where(st => st.ToShopId == shopId);
        }

        return await query
            .OrderByDescending(st => st.InitiatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockTransfer>> GetTransferHistoryAsync(
        string shopId,
        bool isSender = true,
        DateTime? startDate = null,
        DateTime? endDate = null,
        TransferStatus? status = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockTransfers
            .AsNoTracking()
            .Where(st => isSender ? st.FromShopId == shopId : st.ToShopId == shopId);

        if (startDate.HasValue)
            query = query.Where(st => st.InitiatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(st => st.InitiatedAt <= endDate.Value);

        if (status.HasValue)
            query = query.Where(st => st.Status == status.Value);

        query = query.OrderByDescending(st => st.InitiatedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }
}
