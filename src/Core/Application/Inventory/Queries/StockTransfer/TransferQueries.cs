using pos_system_api.Core.Application.Inventory.DTOs;
using MediatR;

namespace pos_system_api.Core.Application.Inventory.Queries.StockTransfer;

public record GetPendingTransfersQuery(
    string ShopId,
    bool IsSender = true
) : IRequest<IEnumerable<StockTransferDto>>;

public record GetTransferHistoryQuery(
    string ShopId,
    bool IsSender = true,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? Status = null,
    int? Limit = 100
) : IRequest<IEnumerable<StockTransferDto>>;
