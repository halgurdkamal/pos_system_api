using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Inventory.Commands.StockTransfer;

// Create Transfer Handler
public class CreateTransferCommandHandler : IRequestHandler<CreateTransferCommand, StockTransferDto>
{
    private readonly IStockTransferRepository _transferRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockAdjustmentRepository _adjustmentRepository;
    private readonly ILogger<CreateTransferCommandHandler> _logger;

    public CreateTransferCommandHandler(
        IStockTransferRepository transferRepository,
        IInventoryRepository inventoryRepository,
        IStockAdjustmentRepository adjustmentRepository,
        ILogger<CreateTransferCommandHandler> logger)
    {
        _transferRepository = transferRepository;
        _inventoryRepository = inventoryRepository;
        _adjustmentRepository = adjustmentRepository;
        _logger = logger;
    }

    public async Task<StockTransferDto> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating transfer from {From} to {To} for drug {Drug}",
            request.FromShopId, request.ToShopId, request.DrugId);

        // Validate source inventory
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(
            request.FromShopId, request.DrugId, cancellationToken);

        if (inventory == null)
            throw new KeyNotFoundException($"Inventory not found for drug {request.DrugId} in shop {request.FromShopId}");

        if (inventory.TotalStock < request.Quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {inventory.TotalStock}, Requested: {request.Quantity}");

        // Create transfer
        var transfer = new Core.Domain.Inventory.Entities.StockTransfer(
            request.FromShopId, request.ToShopId, request.DrugId,
            request.BatchNumber, request.Quantity, request.InitiatedBy, request.Notes);

        var savedTransfer = await _transferRepository.AddAsync(transfer, cancellationToken);

        // Reserve stock (reduce from source, create adjustment)
        var quantityBefore = inventory.TotalStock;
        inventory.ReduceStock(request.Quantity);
        await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

        // Create audit trail
        var adjustment = new StockAdjustment(
            request.FromShopId, request.DrugId, request.BatchNumber,
            AdjustmentType.TransferOut, -request.Quantity, quantityBefore,
            $"Transfer to shop {request.ToShopId}", request.InitiatedBy,
            $"Transfer ID: {savedTransfer.Id}", savedTransfer.Id, "StockTransfer");
        await _adjustmentRepository.AddAsync(adjustment, cancellationToken);

        return MapToDto(savedTransfer);
    }

    private StockTransferDto MapToDto(Core.Domain.Inventory.Entities.StockTransfer transfer) => new()
    {
        Id = transfer.Id,
        FromShopId = transfer.FromShopId,
        ToShopId = transfer.ToShopId,
        DrugId = transfer.DrugId,
        BatchNumber = transfer.BatchNumber,
        Quantity = transfer.Quantity,
        Status = transfer.Status.ToString(),
        InitiatedBy = transfer.InitiatedBy,
        InitiatedAt = transfer.InitiatedAt,
        ApprovedBy = transfer.ApprovedBy,
        ApprovedAt = transfer.ApprovedAt,
        ReceivedBy = transfer.ReceivedBy,
        ReceivedAt = transfer.ReceivedAt,
        CancelledBy = transfer.CancelledBy,
        CancelledAt = transfer.CancelledAt,
        CancellationReason = transfer.CancellationReason,
        Notes = transfer.Notes
    };
}

// Approve Transfer Handler
public class ApproveTransferCommandHandler : IRequestHandler<ApproveTransferCommand, StockTransferDto>
{
    private readonly IStockTransferRepository _repository;
    private readonly ILogger<ApproveTransferCommandHandler> _logger;

    public ApproveTransferCommandHandler(
        IStockTransferRepository repository,
        ILogger<ApproveTransferCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<StockTransferDto> Handle(ApproveTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await _repository.GetByIdAsync(request.TransferId, cancellationToken);
        if (transfer == null)
            throw new KeyNotFoundException($"Transfer {request.TransferId} not found");

        transfer.Approve(request.ApprovedBy);
        transfer.MarkInTransit();
        await _repository.UpdateAsync(transfer, cancellationToken);

        _logger.LogInformation("Transfer {Id} approved and marked in transit", transfer.Id);

        return MapToDto(transfer);
    }

    private StockTransferDto MapToDto(Core.Domain.Inventory.Entities.StockTransfer t) => new()
    {
        Id = t.Id,
        FromShopId = t.FromShopId,
        ToShopId = t.ToShopId,
        DrugId = t.DrugId,
        BatchNumber = t.BatchNumber,
        Quantity = t.Quantity,
        Status = t.Status.ToString(),
        InitiatedBy = t.InitiatedBy,
        InitiatedAt = t.InitiatedAt,
        ApprovedBy = t.ApprovedBy,
        ApprovedAt = t.ApprovedAt,
        ReceivedBy = t.ReceivedBy,
        ReceivedAt = t.ReceivedAt,
        CancelledBy = t.CancelledBy,
        CancelledAt = t.CancelledAt,
        CancellationReason = t.CancellationReason,
        Notes = t.Notes
    };
}

// Receive Transfer Handler
public class ReceiveTransferCommandHandler : IRequestHandler<ReceiveTransferCommand, StockTransferDto>
{
    private readonly IStockTransferRepository _transferRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockAdjustmentRepository _adjustmentRepository;
    private readonly ILogger<ReceiveTransferCommandHandler> _logger;

    public ReceiveTransferCommandHandler(
        IStockTransferRepository transferRepository,
        IInventoryRepository inventoryRepository,
        IStockAdjustmentRepository adjustmentRepository,
        ILogger<ReceiveTransferCommandHandler> logger)
    {
        _transferRepository = transferRepository;
        _inventoryRepository = inventoryRepository;
        _adjustmentRepository = adjustmentRepository;
        _logger = logger;
    }

    public async Task<StockTransferDto> Handle(ReceiveTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await _transferRepository.GetByIdAsync(request.TransferId, cancellationToken);
        if (transfer == null)
            throw new KeyNotFoundException($"Transfer {request.TransferId} not found");

        transfer.Complete(request.ReceivedBy);
        await _transferRepository.UpdateAsync(transfer, cancellationToken);

        // Add to receiving shop inventory (simplified - should match batch details from source)
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(
            transfer.ToShopId, transfer.DrugId, cancellationToken);

        if (inventory != null)
        {
            var quantityBefore = inventory.TotalStock;
            // Note: In real implementation, should transfer actual batch with expiry, prices, etc.
            inventory.RecalculateTotalStock();
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

            // Create audit trail for receiving shop
            var adjustment = new StockAdjustment(
                transfer.ToShopId, transfer.DrugId, transfer.BatchNumber,
                AdjustmentType.TransferIn, transfer.Quantity, quantityBefore,
                $"Transfer from shop {transfer.FromShopId}", request.ReceivedBy,
                $"Transfer ID: {transfer.Id}", transfer.Id, "StockTransfer");
            await _adjustmentRepository.AddAsync(adjustment, cancellationToken);
        }

        _logger.LogInformation("Transfer {Id} completed and received", transfer.Id);

        return MapToDto(transfer);
    }

    private StockTransferDto MapToDto(Core.Domain.Inventory.Entities.StockTransfer t) => new()
    {
        Id = t.Id,
        FromShopId = t.FromShopId,
        ToShopId = t.ToShopId,
        DrugId = t.DrugId,
        BatchNumber = t.BatchNumber,
        Quantity = t.Quantity,
        Status = t.Status.ToString(),
        InitiatedBy = t.InitiatedBy,
        InitiatedAt = t.InitiatedAt,
        ApprovedBy = t.ApprovedBy,
        ApprovedAt = t.ApprovedAt,
        ReceivedBy = t.ReceivedBy,
        ReceivedAt = t.ReceivedAt,
        CancelledBy = t.CancelledBy,
        CancelledAt = t.CancelledAt,
        CancellationReason = t.CancellationReason,
        Notes = t.Notes
    };
}

// Cancel Transfer Handler
public class CancelTransferCommandHandler : IRequestHandler<CancelTransferCommand, StockTransferDto>
{
    private readonly IStockTransferRepository _transferRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockAdjustmentRepository _adjustmentRepository;
    private readonly ILogger<CancelTransferCommandHandler> _logger;

    public CancelTransferCommandHandler(
        IStockTransferRepository transferRepository,
        IInventoryRepository inventoryRepository,
        IStockAdjustmentRepository adjustmentRepository,
        ILogger<CancelTransferCommandHandler> logger)
    {
        _transferRepository = transferRepository;
        _inventoryRepository = inventoryRepository;
        _adjustmentRepository = adjustmentRepository;
        _logger = logger;
    }

    public async Task<StockTransferDto> Handle(CancelTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await _transferRepository.GetByIdAsync(request.TransferId, cancellationToken);
        if (transfer == null)
            throw new KeyNotFoundException($"Transfer {request.TransferId} not found");

        transfer.Cancel(request.CancelledBy, request.Reason);
        await _transferRepository.UpdateAsync(transfer, cancellationToken);

        // Return stock to source shop (create reverse adjustment)
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(
            transfer.FromShopId, transfer.DrugId, cancellationToken);

        if (inventory != null)
        {
            var quantityBefore = inventory.TotalStock;
            // Note: Should restore the exact batch that was reserved
            inventory.RecalculateTotalStock();
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

            var adjustment = new StockAdjustment(
                transfer.FromShopId, transfer.DrugId, transfer.BatchNumber,
                AdjustmentType.Correction, transfer.Quantity, quantityBefore,
                $"Transfer cancelled: {request.Reason}", request.CancelledBy,
                $"Transfer ID: {transfer.Id}", transfer.Id, "StockTransfer");
            await _adjustmentRepository.AddAsync(adjustment, cancellationToken);
        }

        _logger.LogInformation("Transfer {Id} cancelled", transfer.Id);

        return MapToDto(transfer);
    }

    private StockTransferDto MapToDto(Core.Domain.Inventory.Entities.StockTransfer t) => new()
    {
        Id = t.Id,
        FromShopId = t.FromShopId,
        ToShopId = t.ToShopId,
        DrugId = t.DrugId,
        BatchNumber = t.BatchNumber,
        Quantity = t.Quantity,
        Status = t.Status.ToString(),
        InitiatedBy = t.InitiatedBy,
        InitiatedAt = t.InitiatedAt,
        ApprovedBy = t.ApprovedBy,
        ApprovedAt = t.ApprovedAt,
        ReceivedBy = t.ReceivedBy,
        ReceivedAt = t.ReceivedAt,
        CancelledBy = t.CancelledBy,
        CancelledAt = t.CancelledAt,
        CancellationReason = t.CancellationReason,
        Notes = t.Notes
    };
}
