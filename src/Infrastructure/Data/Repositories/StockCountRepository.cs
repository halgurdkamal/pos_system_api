using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.Infrastructure.Data.Repositories;

public class StockCountRepository : IStockCountRepository
{
    private readonly ApplicationDbContext _context;

    public StockCountRepository(ApplicationDbContext context) => _context = context;

    public async Task<StockCount> AddAsync(StockCount stockCount, CancellationToken cancellationToken = default)
    {
        await _context.StockCounts.AddAsync(stockCount, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return stockCount;
    }

    public async Task<StockCount?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        await _context.StockCounts.FirstOrDefaultAsync(sc => sc.Id == id, cancellationToken);

    public async Task<StockCount> UpdateAsync(StockCount stockCount, CancellationToken cancellationToken = default)
    {
        _context.StockCounts.Update(stockCount);
        await _context.SaveChangesAsync(cancellationToken);
        return stockCount;
    }

    public async Task<IEnumerable<StockCount>> GetByShopAsync(string shopId, StockCountStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = _context.StockCounts.AsNoTracking().Where(sc => sc.ShopId == shopId);
        if (status.HasValue) query = query.Where(sc => sc.Status == status.Value);
        return await query.OrderByDescending(sc => sc.ScheduledAt).ToListAsync(cancellationToken);
    }
}
