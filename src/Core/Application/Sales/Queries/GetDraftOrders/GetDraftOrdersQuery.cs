using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Sales.DTOs;
using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Core.Application.Sales.Queries.GetDraftOrders;

/// <summary>
/// Get all draft (pending/paused) orders for a cashier or shop
/// </summary>
public record GetDraftOrdersQuery(
    string? ShopId = null,
    string? CashierId = null
) : IRequest<List<SalesOrderSummaryDto>>;

public class GetDraftOrdersQueryHandler : IRequestHandler<GetDraftOrdersQuery, List<SalesOrderSummaryDto>>
{
    private readonly ISalesOrderRepository _repository;

    public GetDraftOrdersQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SalesOrderSummaryDto>> Handle(GetDraftOrdersQuery request, CancellationToken cancellationToken)
    {
        var (orders, _) = await _repository.GetPagedAsync(
            shopId: request.ShopId,
            cashierId: request.CashierId,
            customerId: null,
            status: SalesOrderStatus.Draft, // Only draft orders
            fromDate: null,
            toDate: null,
            paymentMethod: null,
            searchTerm: null,
            page: 1,
            pageSize: 100, // Get all drafts
            cancellationToken: cancellationToken
        );

        return orders.Select(order => new SalesOrderSummaryDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            ShopId = order.ShopId,
            CustomerName = order.CustomerName,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            OrderDate = order.OrderDate,
            CashierId = order.CashierId,
            ItemCount = order.Items.Count,
            PaymentMethod = order.PaymentMethod?.ToString()
        })
        .OrderBy(o => o.OrderDate) // Oldest first
        .ToList();
    }
}
