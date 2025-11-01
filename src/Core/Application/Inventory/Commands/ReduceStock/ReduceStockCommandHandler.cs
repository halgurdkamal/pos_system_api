using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Services;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Core.Application.Inventory.Commands.ReduceStock;

/// <summary>
/// Handler for ReduceStockCommand
/// Automatically updates packaging prices when a batch is exhausted
/// </summary>
public class ReduceStockCommandHandler : IRequestHandler<ReduceStockCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IStockAdjustmentRepository _adjustmentRepository;
    private readonly IPackagingPricingService _pricingService;
    private readonly ILogger<ReduceStockCommandHandler> _logger;

    public ReduceStockCommandHandler(
        IInventoryRepository inventoryRepository,
        IStockAdjustmentRepository adjustmentRepository,
        IPackagingPricingService pricingService,
        ILogger<ReduceStockCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _adjustmentRepository = adjustmentRepository;
        _pricingService = pricingService;
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

        // Record state before reduction for batch exhaustion detection
        var quantityBefore = inventory.TotalStock;
        var activeBatchesBefore = inventory.Batches
            .Where(b => b.Status == BatchStatus.Active && b.QuantityOnHand > 0)
            .OrderBy(b => b.ReceivedDate)
            .Select(b => new { b.BatchNumber, b.QuantityOnHand })
            .ToList();

        // Reduce stock using FIFO logic (domain method)
        inventory.ReduceStock(request.Quantity);

        // Detect if any batches were exhausted
        var exhaustedBatches = activeBatchesBefore
            .Where(before => 
            {
                var after = inventory.Batches.FirstOrDefault(b => b.BatchNumber == before.BatchNumber);
                return after != null && before.QuantityOnHand > 0 && after.QuantityOnHand == 0;
            })
            .Select(b => b.BatchNumber)
            .ToList();

        // Save changes
        var updatedInventory = await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

        // If batches were exhausted, automatically update packaging prices
        if (exhaustedBatches.Any())
        {
            _logger.LogInformation(
                "Batch(es) exhausted for Shop {ShopId}, Drug {DrugId}: {Batches}. Triggering automatic price update.",
                request.ShopId,
                request.DrugId,
                string.Join(", ", exhaustedBatches));

            try
            {
                // Get the new active batch
                var newActiveBatch = _pricingService.GetActiveBatch(updatedInventory);

                if (newActiveBatch != null)
                {
                    // Automatically update packaging prices from the new active batch
                    var pricingResult = await _pricingService.UpdatePackagingPricesFromBatchAsync(
                        updatedInventory,
                        newActiveBatch,
                        cancellationToken);

                    if (pricingResult.HasChanges)
                    {
                        // Save pricing updates
                        await _inventoryRepository.UpdateAsync(updatedInventory, cancellationToken);

                        _logger.LogInformation(
                            "Automatically updated packaging prices for Shop {ShopId}, Drug {DrugId}. " +
                            "New batch: {BatchNumber}, Auto-calculated: {Count}, Custom kept: {CustomCount}",
                            request.ShopId,
                            request.DrugId,
                            pricingResult.Summary.BatchNumber,
                            pricingResult.Summary.AutoCalculatedCount,
                            pricingResult.Summary.CustomPriceCount);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "No packaging price changes needed for Shop {ShopId}, Drug {DrugId} after batch exhaustion",
                            request.ShopId,
                            request.DrugId);
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "No active batch available for Shop {ShopId}, Drug {DrugId} after exhaustion. " +
                        "Stock is now empty.",
                        request.ShopId,
                        request.DrugId);
                }
            }
            catch (Exception ex)
            {
                // Don't fail the stock reduction if pricing update fails
                _logger.LogError(ex,
                    "Failed to automatically update packaging prices after batch exhaustion for Shop {ShopId}, Drug {DrugId}",
                    request.ShopId,
                    request.DrugId);
            }
        }

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
                exhaustedBatches.Any() 
                    ? $"Stock reduced via ReduceStock command. Batches exhausted: {string.Join(", ", exhaustedBatches)}"
                    : "Stock reduced via ReduceStock command",
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
