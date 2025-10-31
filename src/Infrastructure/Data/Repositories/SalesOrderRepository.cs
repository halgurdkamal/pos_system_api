using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Infrastructure.Data.Repositories;

public class SalesOrderRepository : ISalesOrderRepository
{
    private readonly ApplicationDbContext _context;

    public SalesOrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrders
            .Include(so => so.Items)
            .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);
    }

    public async Task<SalesOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrders
            .Include(so => so.Items)
            .FirstOrDefaultAsync(so => so.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<List<SalesOrder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrders
            .Include(so => so.Items)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SalesOrder>> GetByShopIdAsync(string shopId, CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrders
            .Include(so => so.Items)
            .Where(so => so.ShopId == shopId)
            .AsNoTracking()
            .OrderByDescending(so => so.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SalesOrder>> GetByCashierIdAsync(string cashierId, CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrders
            .Include(so => so.Items)
            .Where(so => so.CashierId == cashierId)
            .AsNoTracking()
            .OrderByDescending(so => so.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default)
    {
        await _context.SalesOrders.AddAsync(salesOrder, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default)
    {
        _context.SalesOrders.Update(salesOrder);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var salesOrder = await _context.SalesOrders.FindAsync(new object[] { id }, cancellationToken);
        if (salesOrder != null)
        {
            _context.SalesOrders.Remove(salesOrder);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(List<SalesOrder> Orders, int TotalCount)> GetPagedAsync(
        string? shopId = null,
        string? cashierId = null,
        string? customerId = null,
        SalesOrderStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        PaymentMethod? paymentMethod = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders
            .Include(so => so.Items)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(shopId))
            query = query.Where(so => so.ShopId == shopId);

        if (!string.IsNullOrEmpty(cashierId))
            query = query.Where(so => so.CashierId == cashierId);

        if (!string.IsNullOrEmpty(customerId))
            query = query.Where(so => so.CustomerId == customerId);

        if (status.HasValue)
            query = query.Where(so => so.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(so => so.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(so => so.OrderDate <= toDate.Value);

        if (paymentMethod.HasValue)
            query = query.Where(so => so.PaymentMethod == paymentMethod.Value);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(so =>
                so.OrderNumber.Contains(searchTerm) ||
                so.CustomerName!.Contains(searchTerm) ||
                so.CustomerPhone!.Contains(searchTerm) ||
                so.Notes!.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(so => so.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }

    public async Task<decimal> GetTotalSalesAsync(
        string shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders
            .Where(so => so.ShopId == shopId &&
                        (so.Status == SalesOrderStatus.Completed || so.Status == SalesOrderStatus.Paid));

        if (fromDate.HasValue)
            query = query.Where(so => so.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(so => so.OrderDate <= toDate.Value);

        return await query.SumAsync(so => so.TotalAmount, cancellationToken);
    }

    public async Task<int> GetOrderCountAsync(
        string shopId,
        SalesOrderStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders.Where(so => so.ShopId == shopId);

        if (status.HasValue)
            query = query.Where(so => so.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(so => so.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(so => so.OrderDate <= toDate.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<SalesOrder>> GetTodaysOrdersAsync(string shopId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.SalesOrders
            .Include(so => so.Items)
            .Where(so => so.ShopId == shopId && so.OrderDate >= today)
            .AsNoTracking()
            .OrderByDescending(so => so.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SalesOrder>> GetRecentOrdersAsync(string shopId, int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.SalesOrders
            .Include(so => so.Items)
            .Where(so => so.ShopId == shopId)
            .AsNoTracking()
            .OrderByDescending(so => so.OrderDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, decimal>> GetCashierSalesAsync(
        string shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders
            .Where(so => so.ShopId == shopId &&
                        (so.Status == SalesOrderStatus.Completed || so.Status == SalesOrderStatus.Paid));

        if (fromDate.HasValue)
            query = query.Where(so => so.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(so => so.OrderDate <= toDate.Value);

        return await query
            .GroupBy(so => so.CashierId)
            .Select(g => new { CashierId = g.Key, Total = g.Sum(so => so.TotalAmount) })
            .ToDictionaryAsync(x => x.CashierId, x => x.Total, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetCashierOrderCountAsync(
        string shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders
            .Where(so => so.ShopId == shopId);

        if (fromDate.HasValue)
            query = query.Where(so => so.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(so => so.OrderDate <= toDate.Value);

        return await query
            .GroupBy(so => so.CashierId)
            .Select(g => new { CashierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CashierId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<string, decimal>> GetSalesByPaymentMethodAsync(
        string shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders
            .Where(so => so.ShopId == shopId &&
                        so.PaymentMethod.HasValue &&
                        (so.Status == SalesOrderStatus.Completed || so.Status == SalesOrderStatus.Paid));

        if (fromDate.HasValue)
            query = query.Where(so => so.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(so => so.OrderDate <= toDate.Value);

        return await query
            .GroupBy(so => so.PaymentMethod!.Value)
            .Select(g => new { PaymentMethod = g.Key.ToString(), Total = g.Sum(so => so.TotalAmount) })
            .ToDictionaryAsync(x => x.PaymentMethod, x => x.Total, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetTopSellingDrugsAsync(
        string shopId,
        int topCount = 10,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.SalesOrders
            .Where(so => so.ShopId == shopId &&
                        (so.Status == SalesOrderStatus.Completed || so.Status == SalesOrderStatus.Paid));

        if (fromDate.HasValue)
            query = query.Where(so => so.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(so => so.OrderDate <= toDate.Value);

        var items = await query
            .SelectMany(so => so.Items)
            .GroupBy(item => item.DrugId)
            .Select(g => new { DrugId = g.Key, TotalQuantity = g.Sum(item => item.Quantity) })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(topCount)
            .ToDictionaryAsync(x => x.DrugId, x => x.TotalQuantity, cancellationToken);

        return items;
    }
}
