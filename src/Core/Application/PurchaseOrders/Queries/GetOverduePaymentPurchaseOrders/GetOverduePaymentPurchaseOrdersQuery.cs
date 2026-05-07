using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;

namespace pos_system_api.Core.Application.PurchaseOrders.Queries.GetOverduePaymentPurchaseOrders;

public record GetOverduePaymentPurchaseOrdersQuery(string ShopId)
    : IRequest<List<PurchaseOrderSummaryDto>>;

public class GetOverduePaymentPurchaseOrdersQueryHandler
    : IRequestHandler<GetOverduePaymentPurchaseOrdersQuery, List<PurchaseOrderSummaryDto>>
{
    private readonly IPurchaseOrderRepository _repository;

    public GetOverduePaymentPurchaseOrdersQueryHandler(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PurchaseOrderSummaryDto>> Handle(
        GetOverduePaymentPurchaseOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _repository.GetOverduePaymentsAsync(request.ShopId, cancellationToken);
        return orders.Select(PurchaseOrderMappers.ToSummaryDto).ToList();
    }
}
