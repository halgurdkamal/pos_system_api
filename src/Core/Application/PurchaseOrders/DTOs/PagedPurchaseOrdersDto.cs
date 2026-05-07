namespace pos_system_api.Core.Application.PurchaseOrders.DTOs;

public record PagedPurchaseOrdersDto
{
    public List<PurchaseOrderSummaryDto> Orders { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
