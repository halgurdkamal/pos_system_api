using MediatR;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;

namespace pos_system_api.Core.Application.PurchaseOrders.Queries.GetPurchaseOrderDashboard;

public record GetPurchaseOrderDashboardQuery(
    string ShopId,
    DateTime? FromDate = null,
    DateTime? ToDate = null) : IRequest<PurchaseOrderDashboardDto>;

public class GetPurchaseOrderDashboardQueryHandler : IRequestHandler<GetPurchaseOrderDashboardQuery, PurchaseOrderDashboardDto>
{
    private readonly IPurchaseOrderRepository _repository;
    private readonly ISupplierRepository _supplierRepository;

    public GetPurchaseOrderDashboardQueryHandler(
        IPurchaseOrderRepository repository,
        ISupplierRepository supplierRepository)
    {
        _repository = repository;
        _supplierRepository = supplierRepository;
    }

    public async Task<PurchaseOrderDashboardDto> Handle(GetPurchaseOrderDashboardQuery request, CancellationToken cancellationToken)
    {
        // Get all metrics in parallel for dashboard performance
        var totalValueTask = _repository.GetTotalOrderValueAsync(request.ShopId, request.FromDate, request.ToDate, cancellationToken);
        var totalCountTask = _repository.GetOrderCountAsync(request.ShopId, null, request.FromDate, request.ToDate, cancellationToken);
        var draftCountTask = _repository.GetOrderCountAsync(request.ShopId, PurchaseOrderStatus.Draft, request.FromDate, request.ToDate, cancellationToken);
        var pendingOrdersTask = _repository.GetPendingOrdersAsync(request.ShopId, cancellationToken);
        var completedCountTask = _repository.GetOrderCountAsync(request.ShopId, PurchaseOrderStatus.Completed, request.FromDate, request.ToDate, cancellationToken);
        var overduePaymentsTask = _repository.GetOverduePaymentsAsync(request.ShopId, cancellationToken);
        var recentOrdersTask = _repository.GetRecentOrdersAsync(request.ShopId, 10, cancellationToken);
        var spendingBySupplierTask = _repository.GetSupplierSpendingAsync(request.ShopId, request.FromDate, request.ToDate, cancellationToken);
        var ordersBySupplierTask = _repository.GetSupplierOrderCountAsync(request.ShopId, request.FromDate, request.ToDate, cancellationToken);

        await Task.WhenAll(
            totalValueTask, totalCountTask, draftCountTask, pendingOrdersTask,
            completedCountTask, overduePaymentsTask, recentOrdersTask,
            spendingBySupplierTask, ordersBySupplierTask);

        var totalValue = await totalValueTask;
        var totalCount = await totalCountTask;
        var draftCount = await draftCountTask;
        var pendingOrders = await pendingOrdersTask;
        var completedCount = await completedCountTask;
        var overduePayments = await overduePaymentsTask;
        var recentOrders = await recentOrdersTask;
        var spendingBySupplier = await spendingBySupplierTask;
        var ordersBySupplier = await ordersBySupplierTask;

        return new PurchaseOrderDashboardDto
        {
            TotalOrderValue = totalValue,
            TotalOrders = totalCount,
            DraftOrders = draftCount,
            PendingOrders = pendingOrders.Count,
            CompletedOrders = completedCount,
            OverduePayments = overduePayments.Count,
            OutstandingPayments = overduePayments.Sum(o => o.TotalAmount),
            RecentOrders = recentOrders.Select(MapToSummaryDto).ToList(),
            SpendingBySupplier = spendingBySupplier,
            OrdersBySupplier = ordersBySupplier
        };
    }

    private static PurchaseOrderSummaryDto MapToSummaryDto(pos_system_api.Core.Domain.PurchaseOrders.Entities.PurchaseOrder po)
    {
        return new PurchaseOrderSummaryDto
        {
            Id = po.Id,
            OrderNumber = po.OrderNumber,
            ShopId = po.ShopId,
            SupplierId = po.SupplierId,
            Status = po.Status.ToString(),
            Priority = po.Priority.ToString(),
            TotalAmount = po.TotalAmount,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            IsPaid = po.IsPaid,
            ItemCount = po.Items.Count,
            CompletionPercentage = po.GetCompletionPercentage()
        };
    }
}
