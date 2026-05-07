using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;

namespace pos_system_api.Core.Application.Sales.Queries.GetTopSellingDrugs;

public record GetTopSellingDrugsQuery(
    string ShopId,
    int TopCount = 10,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Dictionary<string, int>>;

public class GetTopSellingDrugsQueryHandler
    : IRequestHandler<GetTopSellingDrugsQuery, Dictionary<string, int>>
{
    private readonly ISalesOrderRepository _repository;

    public GetTopSellingDrugsQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public Task<Dictionary<string, int>> Handle(
        GetTopSellingDrugsQuery request,
        CancellationToken cancellationToken) =>
        _repository.GetTopSellingDrugsAsync(
            request.ShopId, request.TopCount, request.FromDate, request.ToDate, cancellationToken);
}
