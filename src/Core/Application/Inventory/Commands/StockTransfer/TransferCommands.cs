using pos_system_api.Core.Application.Inventory.DTOs;
using MediatR;

namespace pos_system_api.Core.Application.Inventory.Commands.StockTransfer;

public record CreateTransferCommand(
    string FromShopId,
    string ToShopId,
    string DrugId,
    string? BatchNumber,
    int Quantity,
    string InitiatedBy,
    string? Notes = null
) : IRequest<StockTransferDto>;

public record ApproveTransferCommand(
    string TransferId,
    string ApprovedBy
) : IRequest<StockTransferDto>;

public record ReceiveTransferCommand(
    string TransferId,
    string ReceivedBy
) : IRequest<StockTransferDto>;

public record CancelTransferCommand(
    string TransferId,
    string CancelledBy,
    string Reason
) : IRequest<StockTransferDto>;
