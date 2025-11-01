using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Services;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdatePackagingPrices;

/// <summary>
/// Handler for updating packaging level prices from the active batch
/// </summary>
public class UpdatePackagingPricesFromBatchCommandHandler 
    : IRequestHandler<UpdatePackagingPricesFromBatchCommand, PackagingPricingUpdateResult>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPackagingPricingService _pricingService;
    private readonly ILogger<UpdatePackagingPricesFromBatchCommandHandler> _logger;

    public UpdatePackagingPricesFromBatchCommandHandler(
        IInventoryRepository inventoryRepository,
        IPackagingPricingService pricingService,
        ILogger<UpdatePackagingPricesFromBatchCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _pricingService = pricingService;
        _logger = logger;
    }

    public async Task<PackagingPricingUpdateResult> Handle(
        UpdatePackagingPricesFromBatchCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing packaging price update for Shop {ShopId}, Drug {DrugId}",
            request.ShopId,
            request.DrugId);

        // Get the inventory
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(
            request.ShopId,
            request.DrugId,
            cancellationToken);

        if (inventory == null)
        {
            _logger.LogWarning(
                "Inventory not found for Shop {ShopId}, Drug {DrugId}",
                request.ShopId,
                request.DrugId);

            throw new KeyNotFoundException(
                $"Inventory not found for Shop {request.ShopId} and Drug {request.DrugId}");
        }

        // Get the active batch
        var activeBatch = _pricingService.GetActiveBatch(inventory);

        if (activeBatch == null)
        {
            _logger.LogWarning(
                "No active batch found for Shop {ShopId}, Drug {DrugId}. Cannot update prices.",
                request.ShopId,
                request.DrugId);

            // Return empty result
            return new PackagingPricingUpdateResult
            {
                UpdatedPricing = inventory.ShopPricing,
                Summary = new PackagingPricingUpdateSummary
                {
                    BatchNumber = null,
                    BatchSellingPrice = null
                }
            };
        }

        // Update prices from batch
        var result = await _pricingService.UpdatePackagingPricesFromBatchAsync(
            inventory,
            activeBatch,
            cancellationToken);

        // Save changes if any were made
        if (result.HasChanges)
        {
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

            _logger.LogInformation(
                "Packaging prices updated and saved for Shop {ShopId}, Drug {DrugId}. " +
                "Changes: {ChangeCount}",
                request.ShopId,
                request.DrugId,
                result.Summary.Changes.Count);
        }
        else
        {
            _logger.LogInformation(
                "No packaging price changes needed for Shop {ShopId}, Drug {DrugId}",
                request.ShopId,
                request.DrugId);
        }

        return result;
    }
}
