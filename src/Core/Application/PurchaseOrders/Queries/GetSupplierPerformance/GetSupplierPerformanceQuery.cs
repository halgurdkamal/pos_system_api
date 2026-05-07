using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;

namespace pos_system_api.Core.Application.PurchaseOrders.Queries.GetSupplierPerformance;

public record GetSupplierPerformanceQuery(
    string ShopId,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<List<SupplierPerformanceDto>>;

public class GetSupplierPerformanceQueryHandler
    : IRequestHandler<GetSupplierPerformanceQuery, List<SupplierPerformanceDto>>
{
    private readonly IPurchaseOrderRepository _repository;

    public GetSupplierPerformanceQueryHandler(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SupplierPerformanceDto>> Handle(
        GetSupplierPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        var spendingTask = _repository.GetSupplierSpendingAsync(
            request.ShopId, request.FromDate, request.ToDate, cancellationToken);
        var orderCountTask = _repository.GetSupplierOrderCountAsync(
            request.ShopId, request.FromDate, request.ToDate, cancellationToken);
        var deliveryTimeTask = _repository.GetSupplierAverageDeliveryTimeAsync(
            request.ShopId, cancellationToken);

        await Task.WhenAll(spendingTask, orderCountTask, deliveryTimeTask);

        var spending = await spendingTask;
        var orderCount = await orderCountTask;
        var deliveryTime = await deliveryTimeTask;

        var supplierIds = spending.Keys.Union(orderCount.Keys).ToList();

        return supplierIds
            .Select(supplierId => new SupplierPerformanceDto
            {
                SupplierId = supplierId,
                SupplierName = supplierId, // Future: join supplier repository for friendly name
                TotalOrders = orderCount.GetValueOrDefault(supplierId, 0),
                TotalSpending = spending.GetValueOrDefault(supplierId, 0),
                AverageDeliveryDays = deliveryTime.GetValueOrDefault(supplierId, 0),
                CompletedOrders = 0, // Future: from detailed data
                CancelledOrders = 0, // Future: from detailed data
                OnTimeDeliveryRate = 0, // Future: from detailed data
            })
            .OrderByDescending(p => p.TotalSpending)
            .ToList();
    }
}
