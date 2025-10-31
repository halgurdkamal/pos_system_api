using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.Infrastructure.Data.Repositories;

public class StockAdjustmentRepository : IStockAdjustmentRepository
{
    private readonly ApplicationDbContext _context;

    public StockAdjustmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StockAdjustment> AddAsync(StockAdjustment adjustment, CancellationToken cancellationToken = default)
    {
        await _context.StockAdjustments.AddAsync(adjustment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return adjustment;
    }

    public async Task<StockAdjustment?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.StockAdjustments
            .AsNoTracking()
            .FirstOrDefaultAsync(sa => sa.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<StockAdjustment>> GetByShopAsync(
        string shopId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        AdjustmentType? adjustmentType = null,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockAdjustments
            .AsNoTracking()
            .Where(sa => sa.ShopId == shopId);

        if (startDate.HasValue)
            query = query.Where(sa => sa.AdjustedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(sa => sa.AdjustedAt <= endDate.Value);

        if (adjustmentType.HasValue)
            query = query.Where(sa => sa.AdjustmentType == adjustmentType.Value);

        query = query.OrderByDescending(sa => sa.AdjustedAt);

        if (limit.HasValue)
            query = query.Take(limit.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<StockAdjustment>> GetByDrugAsync(
        string shopId,
        string drugId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockAdjustments
            .AsNoTracking()
            .Where(sa => sa.ShopId == shopId && sa.DrugId == drugId);

        if (startDate.HasValue)
            query = query.Where(sa => sa.AdjustedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(sa => sa.AdjustedAt <= endDate.Value);

        return await query
            .OrderByDescending(sa => sa.AdjustedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalAdjustmentsCountAsync(
        string shopId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.StockAdjustments
            .Where(sa => sa.ShopId == shopId);

        if (startDate.HasValue)
            query = query.Where(sa => sa.AdjustedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(sa => sa.AdjustedAt <= endDate.Value);

        return await query.CountAsync(cancellationToken);
    }
}
