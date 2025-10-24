using pos_system_api.Core.Application.Inventory.DTOs;
using MediatR;

namespace pos_system_api.Core.Application.Inventory.Queries.GetAdjustmentHistory;

/// <summary>
/// Query to get stock adjustment history for a shop
/// </summary>
public record GetAdjustmentHistoryQuery(
    string ShopId,
    string? DrugId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? AdjustmentType = null,
    int? Limit = 100
) : IRequest<IEnumerable<StockAdjustmentDto>>;
