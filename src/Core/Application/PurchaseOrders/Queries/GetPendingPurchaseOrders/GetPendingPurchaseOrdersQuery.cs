using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;

namespace pos_system_api.Core.Application.PurchaseOrders.Queries.GetPendingPurchaseOrders;

public record GetPendingPurchaseOrdersQuery(string ShopId)
    : IRequest<List<PurchaseOrderSummaryDto>>;

public class GetPendingPurchaseOrdersQueryHandler
    : IRequestHandler<GetPendingPurchaseOrdersQuery, List<PurchaseOrderSummaryDto>>
{
    private readonly IPurchaseOrderRepository _repository;

    public GetPendingPurchaseOrdersQueryHandler(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PurchaseOrderSummaryDto>> Handle(
        GetPendingPurchaseOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _repository.GetPendingOrdersAsync(request.ShopId, cancellationToken);
        return orders.Select(PurchaseOrderMappers.ToSummaryDto).ToList();
    }
}
