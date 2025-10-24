using MediatR;
using pos_system_api.Core.Application.Sales.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Core.Application.Sales.Queries.GetSalesDashboard;

public record GetSalesDashboardQuery(
    string ShopId,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<SalesOrderDashboardDto>;

public class GetSalesDashboardQueryHandler : IRequestHandler<GetSalesDashboardQuery, SalesOrderDashboardDto>
{
    private readonly ISalesOrderRepository _repository;

    public GetSalesDashboardQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<SalesOrderDashboardDto> Handle(GetSalesDashboardQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        
        // Get all metrics in parallel for dashboard performance
        var totalSalesTask = _repository.GetTotalSalesAsync(request.ShopId, request.FromDate, request.ToDate, cancellationToken);
        var totalCountTask = _repository.GetOrderCountAsync(request.ShopId, null, request.FromDate, request.ToDate, cancellationToken);
        var todaysOrdersTask = _repository.GetTodaysOrdersAsync(request.ShopId, cancellationToken);
        var completedCountTask = _repository.GetOrderCountAsync(request.ShopId, SalesOrderStatus.Completed, request.FromDate, request.ToDate, cancellationToken);
        var cancelledCountTask = _repository.GetOrderCountAsync(request.ShopId, SalesOrderStatus.Cancelled, request.FromDate, request.ToDate, cancellationToken);
        var recentOrdersTask = _repository.GetRecentOrdersAsync(request.ShopId, 10, cancellationToken);
        var cashierSalesTask = _repository.GetCashierSalesAsync(request.ShopId, request.FromDate, request.ToDate, cancellationToken);
        var paymentMethodsTask = _repository.GetSalesByPaymentMethodAsync(request.ShopId, request.FromDate, request.ToDate, cancellationToken);
        var topDrugsTask = _repository.GetTopSellingDrugsAsync(request.ShopId, 10, request.FromDate, request.ToDate, cancellationToken);

        await Task.WhenAll(
            totalSalesTask, totalCountTask, todaysOrdersTask, completedCountTask,
            cancelledCountTask, recentOrdersTask, cashierSalesTask, paymentMethodsTask, topDrugsTask);

        var totalSales = await totalSalesTask;
        var totalCount = await totalCountTask;
        var todaysOrders = await todaysOrdersTask;
        var completedCount = await completedCountTask;
        var cancelledCount = await cancelledCountTask;
        var recentOrders = await recentOrdersTask;
        var cashierSales = await cashierSalesTask;
        var paymentMethods = await paymentMethodsTask;
        var topDrugs = await topDrugsTask;

        var todaysSales = todaysOrders
            .Where(o => o.Status == SalesOrderStatus.Completed || o.Status == SalesOrderStatus.Paid)
            .Sum(o => o.TotalAmount);

        var averageOrderValue = totalCount > 0 ? totalSales / totalCount : 0;

        return new SalesOrderDashboardDto
        {
            TotalSales = totalSales,
            TotalOrders = totalCount,
            TodaysOrders = todaysOrders.Count,
            TodaysSales = todaysSales,
            CompletedOrders = completedCount,
            CancelledOrders = cancelledCount,
            AverageOrderValue = averageOrderValue,
            RecentOrders = recentOrders.Select(MapToSummaryDto).ToList(),
            SalesByCashier = cashierSales,
            SalesByPaymentMethod = paymentMethods,
            TopSellingDrugs = topDrugs
        };
    }

    private static SalesOrderSummaryDto MapToSummaryDto(SalesOrder so)
    {
        return new SalesOrderSummaryDto
        {
            Id = so.Id,
            OrderNumber = so.OrderNumber,
            ShopId = so.ShopId,
            CustomerName = so.CustomerName,
            Status = so.Status.ToString(),
            TotalAmount = so.TotalAmount,
            OrderDate = so.OrderDate,
            CashierId = so.CashierId,
            ItemCount = so.Items.Count,
            PaymentMethod = so.PaymentMethod?.ToString()
        };
    }
}
