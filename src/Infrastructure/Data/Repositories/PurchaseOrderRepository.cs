using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;

namespace pos_system_api.Infrastructure.Data.Repositories;

public class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly ApplicationDbContext _context;

    public PurchaseOrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PurchaseOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Items)
                .ThenInclude(i => i.Receipts)
            .FirstOrDefaultAsync(po => po.Id == id, cancellationToken);
    }

    public async Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Items)
                .ThenInclude(i => i.Receipts)
            .FirstOrDefaultAsync(po => po.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task<List<PurchaseOrder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Items)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PurchaseOrder>> GetByShopIdAsync(string shopId, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Items)
            .Where(po => po.ShopId == shopId)
            .AsNoTracking()
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PurchaseOrder>> GetBySupplierIdAsync(string supplierId, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Items)
            .Where(po => po.SupplierId == supplierId)
            .AsNoTracking()
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        await _context.PurchaseOrders.AddAsync(purchaseOrder, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        _context.PurchaseOrders.Update(purchaseOrder);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders.FindAsync(new object[] { id }, cancellationToken);
        if (purchaseOrder != null)
        {
            _context.PurchaseOrders.Remove(purchaseOrder);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<(List<PurchaseOrder> Orders, int TotalCount)> GetPagedAsync(
        string? shopId = null,
        string? supplierId = null,
        PurchaseOrderStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        OrderPriority? priority = null,
        bool? isPaid = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.Items)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(shopId))
            query = query.Where(po => po.ShopId == shopId);

        if (!string.IsNullOrEmpty(supplierId))
            query = query.Where(po => po.SupplierId == supplierId);

        if (status.HasValue)
            query = query.Where(po => po.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(po => po.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(po => po.OrderDate <= toDate.Value);

        if (priority.HasValue)
            query = query.Where(po => po.Priority == priority.Value);

        if (isPaid.HasValue)
            query = query.Where(po => po.IsPaid == isPaid.Value);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(po =>
                po.OrderNumber.Contains(searchTerm) ||
                po.Notes!.Contains(searchTerm) ||
                po.ReferenceNumber!.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var orders = await query
            .OrderByDescending(po => po.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (orders, totalCount);
    }

    public async Task<decimal> GetTotalOrderValueAsync(
        string shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .Where(po => po.ShopId == shopId && po.Status != PurchaseOrderStatus.Cancelled);

        if (fromDate.HasValue)
            query = query.Where(po => po.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(po => po.OrderDate <= toDate.Value);

        return await query.SumAsync(po => po.TotalAmount, cancellationToken);
    }

    public async Task<int> GetOrderCountAsync(
        string shopId,
        PurchaseOrderStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders.Where(po => po.ShopId == shopId);

        if (status.HasValue)
            query = query.Where(po => po.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(po => po.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(po => po.OrderDate <= toDate.Value);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<PurchaseOrder>> GetPendingOrdersAsync(string shopId, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Items)
            .Where(po => po.ShopId == shopId &&
                        (po.Status == PurchaseOrderStatus.Submitted ||
                         po.Status == PurchaseOrderStatus.Confirmed ||
                         po.Status == PurchaseOrderStatus.PartiallyReceived))
            .AsNoTracking()
            .OrderBy(po => po.ExpectedDeliveryDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PurchaseOrder>> GetOverduePaymentsAsync(string shopId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.PurchaseOrders
            .Where(po => po.ShopId == shopId &&
                        !po.IsPaid &&
                        po.PaymentDueDate.HasValue &&
                        po.PaymentDueDate.Value < now &&
                        po.Status != PurchaseOrderStatus.Cancelled)
            .AsNoTracking()
            .OrderBy(po => po.PaymentDueDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PurchaseOrder>> GetRecentOrdersAsync(string shopId, int count = 10, CancellationToken cancellationToken = default)
    {
        return await _context.PurchaseOrders
            .Include(po => po.Items)
            .Where(po => po.ShopId == shopId)
            .AsNoTracking()
            .OrderByDescending(po => po.OrderDate)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<string, decimal>> GetSupplierSpendingAsync(
        string shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .Where(po => po.ShopId == shopId && po.Status != PurchaseOrderStatus.Cancelled);

        if (fromDate.HasValue)
            query = query.Where(po => po.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(po => po.OrderDate <= toDate.Value);

        return await query
            .GroupBy(po => po.SupplierId)
            .Select(g => new { SupplierId = g.Key, Total = g.Sum(po => po.TotalAmount) })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Total, cancellationToken);
    }

    public async Task<Dictionary<string, int>> GetSupplierOrderCountAsync(
        string shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .Where(po => po.ShopId == shopId && po.Status != PurchaseOrderStatus.Cancelled);

        if (fromDate.HasValue)
            query = query.Where(po => po.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(po => po.OrderDate <= toDate.Value);

        return await query
            .GroupBy(po => po.SupplierId)
            .Select(g => new { SupplierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SupplierId, x => x.Count, cancellationToken);
    }

    public async Task<Dictionary<string, double>> GetSupplierAverageDeliveryTimeAsync(
        string shopId,
        CancellationToken cancellationToken = default)
    {
        var completedOrders = await _context.PurchaseOrders
            .Where(po => po.ShopId == shopId &&
                        po.Status == PurchaseOrderStatus.Completed &&
                        po.CompletedAt.HasValue)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return completedOrders
            .GroupBy(po => po.SupplierId)
            .ToDictionary(
                g => g.Key,
                g => g.Average(po => (po.CompletedAt!.Value - po.OrderDate).TotalDays));
    }
}
