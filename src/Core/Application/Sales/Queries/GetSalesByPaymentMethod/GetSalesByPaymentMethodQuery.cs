using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;

namespace pos_system_api.Core.Application.Sales.Queries.GetSalesByPaymentMethod;

public record GetSalesByPaymentMethodQuery(
    string ShopId,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Dictionary<string, decimal>>;

public class GetSalesByPaymentMethodQueryHandler
    : IRequestHandler<GetSalesByPaymentMethodQuery, Dictionary<string, decimal>>
{
    private readonly ISalesOrderRepository _repository;

    public GetSalesByPaymentMethodQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public Task<Dictionary<string, decimal>> Handle(
        GetSalesByPaymentMethodQuery request,
        CancellationToken cancellationToken) =>
        _repository.GetSalesByPaymentMethodAsync(
            request.ShopId, request.FromDate, request.ToDate, cancellationToken);
}
