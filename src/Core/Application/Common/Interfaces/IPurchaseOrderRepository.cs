using pos_system_api.Core.Domain.PurchaseOrders.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Repository for purchase order data access with analytics support
/// </summary>
public interface IPurchaseOrderRepository
{
    // Basic CRUD
    Task<PurchaseOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<PurchaseOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<List<PurchaseOrder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<PurchaseOrder>> GetByShopIdAsync(string shopId, CancellationToken cancellationToken = default);
    Task<List<PurchaseOrder>> GetBySupplierIdAsync(string supplierId, CancellationToken cancellationToken = default);
    Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);
    Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    // Filtering and searching
    Task<(List<PurchaseOrder> Orders, int TotalCount)> GetPagedAsync(
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
        CancellationToken cancellationToken = default);

    // Analytics queries for dashboard
    Task<decimal> GetTotalOrderValueAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<int> GetOrderCountAsync(string shopId, PurchaseOrderStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<List<PurchaseOrder>> GetPendingOrdersAsync(string shopId, CancellationToken cancellationToken = default);
    Task<List<PurchaseOrder>> GetOverduePaymentsAsync(string shopId, CancellationToken cancellationToken = default);
    Task<List<PurchaseOrder>> GetRecentOrdersAsync(string shopId, int count = 10, CancellationToken cancellationToken = default);

    // Supplier performance analytics
    Task<Dictionary<string, decimal>> GetSupplierSpendingAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetSupplierOrderCountAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, double>> GetSupplierAverageDeliveryTimeAsync(string shopId, CancellationToken cancellationToken = default);
}
