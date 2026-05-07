using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Sales.DTOs;

namespace pos_system_api.Core.Application.Sales.Queries.GetTodaysSalesOrders;

public record GetTodaysSalesOrdersQuery(string ShopId)
    : IRequest<List<SalesOrderSummaryDto>>;

public class GetTodaysSalesOrdersQueryHandler
    : IRequestHandler<GetTodaysSalesOrdersQuery, List<SalesOrderSummaryDto>>
{
    private readonly ISalesOrderRepository _repository;

    public GetTodaysSalesOrdersQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SalesOrderSummaryDto>> Handle(
        GetTodaysSalesOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _repository.GetTodaysOrdersAsync(request.ShopId, cancellationToken);
        return orders.Select(SalesOrderMappers.ToSummaryDto).ToList();
    }
}
