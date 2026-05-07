using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;

namespace pos_system_api.Core.Application.PurchaseOrders.Queries.GetPurchaseOrders;

public record GetPurchaseOrdersQuery(
    string? ShopId = null,
    string? SupplierId = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? Priority = null,
    bool? IsPaid = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedPurchaseOrdersDto>;

public class GetPurchaseOrdersQueryHandler
    : IRequestHandler<GetPurchaseOrdersQuery, PagedPurchaseOrdersDto>
{
    private readonly IPurchaseOrderRepository _repository;

    public GetPurchaseOrdersQueryHandler(IPurchaseOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedPurchaseOrdersDto> Handle(
        GetPurchaseOrdersQuery request,
        CancellationToken cancellationToken)
    {
        PurchaseOrderStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<PurchaseOrderStatus>(request.Status, ignoreCase: true, out var s))
        {
            statusEnum = s;
        }

        OrderPriority? priorityEnum = null;
        if (!string.IsNullOrEmpty(request.Priority)
            && Enum.TryParse<OrderPriority>(request.Priority, ignoreCase: true, out var p))
        {
            priorityEnum = p;
        }

        var (orders, totalCount) = await _repository.GetPagedAsync(
            request.ShopId,
            request.SupplierId,
            statusEnum,
            request.FromDate,
            request.ToDate,
            priorityEnum,
            request.IsPaid,
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedPurchaseOrdersDto
        {
            Orders = orders.Select(PurchaseOrderMappers.ToSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
        };
    }
}
