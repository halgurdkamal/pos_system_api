using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Inventory.Queries.StockTransfer;

public class GetPendingTransfersQueryHandler : IRequestHandler<GetPendingTransfersQuery, IEnumerable<StockTransferDto>>
{
    private readonly IStockTransferRepository _repository;

    public GetPendingTransfersQueryHandler(IStockTransferRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<StockTransferDto>> Handle(GetPendingTransfersQuery request, CancellationToken cancellationToken)
    {
        var transfers = await _repository.GetPendingTransfersAsync(request.ShopId, request.IsSender, cancellationToken);
        return transfers.Select(MapToDto);
    }

    private StockTransferDto MapToDto(Core.Domain.Inventory.Entities.StockTransfer t) => new()
    {
        Id = t.Id, FromShopId = t.FromShopId, ToShopId = t.ToShopId, DrugId = t.DrugId,
        BatchNumber = t.BatchNumber, Quantity = t.Quantity, Status = t.Status.ToString(),
        InitiatedBy = t.InitiatedBy, InitiatedAt = t.InitiatedAt, ApprovedBy = t.ApprovedBy,
        ApprovedAt = t.ApprovedAt, ReceivedBy = t.ReceivedBy, ReceivedAt = t.ReceivedAt,
        Notes = t.Notes
    };
}

public class GetTransferHistoryQueryHandler : IRequestHandler<GetTransferHistoryQuery, IEnumerable<StockTransferDto>>
{
    private readonly IStockTransferRepository _repository;

    public GetTransferHistoryQueryHandler(IStockTransferRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<StockTransferDto>> Handle(GetTransferHistoryQuery request, CancellationToken cancellationToken)
    {
        TransferStatus? status = null;
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<TransferStatus>(request.Status, true, out var parsedStatus))
            status = parsedStatus;

        var transfers = await _repository.GetTransferHistoryAsync(
            request.ShopId, request.IsSender, request.StartDate, request.EndDate, status, request.Limit, cancellationToken);
        
        return transfers.Select(MapToDto);
    }

    private StockTransferDto MapToDto(Core.Domain.Inventory.Entities.StockTransfer t) => new()
    {
        Id = t.Id, FromShopId = t.FromShopId, ToShopId = t.ToShopId, DrugId = t.DrugId,
        BatchNumber = t.BatchNumber, Quantity = t.Quantity, Status = t.Status.ToString(),
        InitiatedBy = t.InitiatedBy, InitiatedAt = t.InitiatedAt, ApprovedBy = t.ApprovedBy,
        ApprovedAt = t.ApprovedAt, ReceivedBy = t.ReceivedBy, ReceivedAt = t.ReceivedAt,
        CancelledBy = t.CancelledBy, CancelledAt = t.CancelledAt, Notes = t.Notes
    };
}
