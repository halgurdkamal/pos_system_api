using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Repository for sales order (cashier order) data access with analytics support
/// </summary>
public interface ISalesOrderRepository
{
    // Basic CRUD
    Task<SalesOrder?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<SalesOrder?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<List<SalesOrder>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<SalesOrder>> GetByShopIdAsync(string shopId, CancellationToken cancellationToken = default);
    Task<List<SalesOrder>> GetByCashierIdAsync(string cashierId, CancellationToken cancellationToken = default);
    Task AddAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default);
    Task UpdateAsync(SalesOrder salesOrder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    // Filtering and searching
    Task<(List<SalesOrder> Orders, int TotalCount)> GetPagedAsync(
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
        CancellationToken cancellationToken = default);

    // Analytics queries for dashboard
    Task<decimal> GetTotalSalesAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<int> GetOrderCountAsync(string shopId, SalesOrderStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<List<SalesOrder>> GetTodaysOrdersAsync(string shopId, CancellationToken cancellationToken = default);
    Task<List<SalesOrder>> GetRecentOrdersAsync(string shopId, int count = 10, CancellationToken cancellationToken = default);
    
    // Cashier performance analytics
    Task<Dictionary<string, decimal>> GetCashierSalesAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetCashierOrderCountAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    
    // Sales analytics
    Task<Dictionary<string, decimal>> GetSalesByPaymentMethodAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetTopSellingDrugsAsync(string shopId, int topCount = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
}
