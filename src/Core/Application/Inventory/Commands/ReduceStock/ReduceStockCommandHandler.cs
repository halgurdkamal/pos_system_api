using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Inventory.Commands.ReduceStock;

/// <summary>
/// Handler for ReduceStockCommand
/// </summary>
public class ReduceStockCommandHandler : IRequestHandler<ReduceStockCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockAdjustmentRepository _adjustmentRepository;
    private readonly ILogger<ReduceStockCommandHandler> _logger;

    public ReduceStockCommandHandler(
        IInventoryRepository inventoryRepository,
        IStockAdjustmentRepository adjustmentRepository,
        ILogger<ReduceStockCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _adjustmentRepository = adjustmentRepository;
        _logger = logger;
    }

    public async Task<InventoryDto> Handle(ReduceStockCommand request, CancellationToken cancellationToken)
    {
        // Get inventory
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(
            request.ShopId,
            request.DrugId,
            cancellationToken
        );

        if (inventory == null)
        {
            throw new InvalidOperationException(
                $"Inventory not found for Shop '{request.ShopId}' and Drug '{request.DrugId}'."
            );
        }

        // Validate sufficient stock
        if (inventory.TotalStock < request.Quantity)
        {
            throw new InvalidOperationException(
                $"Insufficient stock. Available: {inventory.TotalStock}, Requested: {request.Quantity}"
            );
        }

        // Record quantity before reduction
        var quantityBefore = inventory.TotalStock;

        // Reduce stock using FIFO logic (domain method)
        inventory.ReduceStock(request.Quantity);

        // Save changes
        var updatedInventory = await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

        // Create audit trail record
        try
        {
            var adjustment = new StockAdjustment(
                request.ShopId,
                request.DrugId,
                null,  // Batch number handled by FIFO logic
                AdjustmentType.Sale,
                -request.Quantity,  // Negative for reduction
                quantityBefore,
                "Stock reduced via ReduceStock command",
                "System",  // TODO: Get from authenticated user
                null,
                null,
                "ReduceStock"
            );

            await _adjustmentRepository.AddAsync(adjustment, cancellationToken);
            _logger.LogInformation("Stock adjustment recorded for reduction: {Id}", adjustment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create stock adjustment record for reduction");
            // Don't fail the operation if audit trail fails
        }

        // Map to DTO
        return InventoryMapper.MapToDto(updatedInventory);
    }
}
