using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Inventory.Commands.CreateStockAdjustment;

public class CreateStockAdjustmentCommandHandler : IRequestHandler<CreateStockAdjustmentCommand, StockAdjustmentDto>
{
    private readonly IStockAdjustmentRepository _adjustmentRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<CreateStockAdjustmentCommandHandler> _logger;

    public CreateStockAdjustmentCommandHandler(
        IStockAdjustmentRepository adjustmentRepository,
        IInventoryRepository inventoryRepository,
        ILogger<CreateStockAdjustmentCommandHandler> logger)
    {
        _adjustmentRepository = adjustmentRepository;
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<StockAdjustmentDto> Handle(CreateStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating stock adjustment for drug {DrugId} in shop {ShopId}: {Type} {Quantity}",
            request.DrugId, request.ShopId, request.AdjustmentType, request.QuantityChanged);

        // Get current inventory to record quantity before adjustment
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(
            request.ShopId,
            request.DrugId,
            cancellationToken);

        if (inventory == null)
        {
            _logger.LogWarning("Inventory not found for drug {DrugId} in shop {ShopId}", 
                request.DrugId, request.ShopId);
            throw new KeyNotFoundException($"Inventory not found for drug {request.DrugId} in shop {request.ShopId}");
        }

        // Parse adjustment type
        if (!Enum.TryParse<AdjustmentType>(request.AdjustmentType, true, out var adjustmentType))
        {
            throw new ArgumentException($"Invalid adjustment type: {request.AdjustmentType}");
        }

        // Get quantity before adjustment
        int quantityBefore = inventory.TotalStock;

        // Create adjustment record
        var adjustment = new StockAdjustment(
            request.ShopId,
            request.DrugId,
            request.BatchNumber,
            adjustmentType,
            request.QuantityChanged,
            quantityBefore,
            request.Reason,
            request.AdjustedBy,
            request.Notes,
            request.ReferenceId,
            request.ReferenceType
        );

        // Save adjustment
        var savedAdjustment = await _adjustmentRepository.AddAsync(adjustment, cancellationToken);

        _logger.LogInformation("Stock adjustment created: {Id}. Before: {Before}, Changed: {Changed}, After: {After}",
            savedAdjustment.Id, quantityBefore, request.QuantityChanged, savedAdjustment.QuantityAfter);

        // Map to DTO
        return new StockAdjustmentDto
        {
            Id = savedAdjustment.Id,
            ShopId = savedAdjustment.ShopId,
            DrugId = savedAdjustment.DrugId,
            BatchNumber = savedAdjustment.BatchNumber,
            AdjustmentType = savedAdjustment.AdjustmentType.ToString(),
            QuantityChanged = savedAdjustment.QuantityChanged,
            QuantityBefore = savedAdjustment.QuantityBefore,
            QuantityAfter = savedAdjustment.QuantityAfter,
            Reason = savedAdjustment.Reason,
            Notes = savedAdjustment.Notes,
            AdjustedBy = savedAdjustment.AdjustedBy,
            AdjustedAt = savedAdjustment.AdjustedAt,
            ReferenceId = savedAdjustment.ReferenceId,
            ReferenceType = savedAdjustment.ReferenceType
        };
    }
}
