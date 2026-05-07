using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Sales.DTOs;
using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Core.Application.Sales.Queries.GetSalesOrders;

public record GetSalesOrdersQuery(
    string? ShopId = null,
    string? CashierId = null,
    string? CustomerId = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? PaymentMethod = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedSalesOrdersDto>;

public class GetSalesOrdersQueryHandler
    : IRequestHandler<GetSalesOrdersQuery, PagedSalesOrdersDto>
{
    private readonly ISalesOrderRepository _repository;

    public GetSalesOrdersQueryHandler(ISalesOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedSalesOrdersDto> Handle(
        GetSalesOrdersQuery request,
        CancellationToken cancellationToken)
    {
        SalesOrderStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<SalesOrderStatus>(request.Status, ignoreCase: true, out var s))
        {
            statusEnum = s;
        }

        PaymentMethod? paymentMethodEnum = null;
        if (!string.IsNullOrEmpty(request.PaymentMethod)
            && Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var pm))
        {
            paymentMethodEnum = pm;
        }

        var (orders, totalCount) = await _repository.GetPagedAsync(
            request.ShopId,
            request.CashierId,
            request.CustomerId,
            statusEnum,
            request.FromDate,
            request.ToDate,
            paymentMethodEnum,
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedSalesOrdersDto
        {
            Orders = orders.Select(SalesOrderMappers.ToSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
        };
    }
}
