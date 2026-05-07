using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Sales.DTOs;

namespace pos_system_api.Core.Application.Sales.Queries.GetCashierPerformance;

public record GetCashierPerformanceQuery(
    string ShopId,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<List<CashierPerformanceDto>>;

public class GetCashierPerformanceQueryHandler
    : IRequestHandler<GetCashierPerformanceQuery, List<CashierPerformanceDto>>
{
    private readonly ISalesOrderRepository _repository;

    public GetCashierPerformanceQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CashierPerformanceDto>> Handle(
        GetCashierPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        var salesTask = _repository.GetCashierSalesAsync(
            request.ShopId, request.FromDate, request.ToDate, cancellationToken);
        var orderCountTask = _repository.GetCashierOrderCountAsync(
            request.ShopId, request.FromDate, request.ToDate, cancellationToken);

        var today = DateTime.UtcNow.Date;
        var todaySalesTask = _repository.GetCashierSalesAsync(
            request.ShopId, today, null, cancellationToken);
        var todayOrderCountTask = _repository.GetCashierOrderCountAsync(
            request.ShopId, today, null, cancellationToken);

        await Task.WhenAll(salesTask, orderCountTask, todaySalesTask, todayOrderCountTask);

        var sales = await salesTask;
        var orderCount = await orderCountTask;
        var todaySales = await todaySalesTask;
        var todayOrderCount = await todayOrderCountTask;

        var cashierIds = sales.Keys.Union(orderCount.Keys).ToList();

        return cashierIds
            .Select(cashierId =>
            {
                var totalSales = sales.GetValueOrDefault(cashierId, 0);
                var totalOrders = orderCount.GetValueOrDefault(cashierId, 0);
                return new CashierPerformanceDto
                {
                    CashierId = cashierId,
                    CashierName = cashierId, // Future: join user repository for friendly name
                    TotalOrders = totalOrders,
                    TotalSales = totalSales,
                    AverageOrderValue = totalOrders > 0 ? totalSales / totalOrders : 0,
                    TodaysOrders = todayOrderCount.GetValueOrDefault(cashierId, 0),
                    TodaysSales = todaySales.GetValueOrDefault(cashierId, 0),
                };
            })
            .OrderByDescending(p => p.TotalSales)
            .ToList();
    }
}
