using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Core.Application.Inventory.Services;

/// <summary>
/// Service for managing automatic pricing updates for packaging levels from active batches
/// </summary>
public class PackagingPricingService : IPackagingPricingService
{
    private readonly IEffectivePackagingService _effectivePackagingService;
    private readonly ILogger<PackagingPricingService> _logger;

    public PackagingPricingService(
        IEffectivePackagingService effectivePackagingService,
        ILogger<PackagingPricingService> logger)
    {
        _effectivePackagingService = effectivePackagingService;
        _logger = logger;
    }

    /// <summary>
    /// Update packaging level prices from the active batch
    /// </summary>
    public async Task<PackagingPricingUpdateResult> UpdatePackagingPricesFromBatchAsync(
        ShopInventory inventory,
        Batch activeBatch,
        CancellationToken cancellationToken = default)
    {
        if (inventory == null)
            throw new ArgumentNullException(nameof(inventory));
        if (activeBatch == null)
            throw new ArgumentNullException(nameof(activeBatch));

        _logger.LogInformation(
            "Updating packaging prices for Shop {ShopId}, Drug {DrugId} from batch {BatchNumber}",
            inventory.ShopId,
            inventory.DrugId,
            activeBatch.BatchNumber);

        var result = new PackagingPricingUpdateResult
        {
            Summary = new PackagingPricingUpdateSummary
            {
                BatchNumber = activeBatch.BatchNumber,
                BatchSellingPrice = activeBatch.SellingPrice
            }
        };

        // Get effective packaging levels for this shop/drug
        EffectivePackagingDto effectivePackaging;
        try
        {
            effectivePackaging = await _effectivePackagingService.GetEffectivePackagingAsync(
                inventory.ShopId,
                inventory.DrugId,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to retrieve effective packaging for Shop {ShopId}, Drug {DrugId}",
                inventory.ShopId,
                inventory.DrugId);
            throw;
        }

        if (effectivePackaging.PackagingLevels == null || !effectivePackaging.PackagingLevels.Any())
        {
            _logger.LogWarning(
                "No packaging levels found for Shop {ShopId}, Drug {DrugId}",
                inventory.ShopId,
                inventory.DrugId);
            
            result.UpdatedPricing = inventory.ShopPricing;
            return result;
        }

        // Initialize PackagingLevelPrices dictionary if null
        if (inventory.ShopPricing.PackagingLevelPrices == null)
        {
            inventory.ShopPricing.PackagingLevelPrices = new Dictionary<string, decimal>();
        }

        // Create a working copy of the dictionary
        var updatedPrices = new Dictionary<string, decimal>(
            inventory.ShopPricing.PackagingLevelPrices,
            StringComparer.OrdinalIgnoreCase);

        result.Summary.TotalLevels = effectivePackaging.PackagingLevels.Count;

        // Process each packaging level
        foreach (var level in effectivePackaging.PackagingLevels)
        {
            var unitName = level.UnitName;
            var effectiveBaseUnits = level.EffectiveBaseUnitQuantity;

            // Check if this packaging level exists in the dictionary
            var existsInDictionary = updatedPrices.TryGetValue(unitName, out var currentPrice);

            if (!existsInDictionary)
            {
                // Add missing packaging level with auto-calculated price
                var calculatedPrice = CalculatePrice(activeBatch.SellingPrice, effectiveBaseUnits);
                updatedPrices[unitName] = calculatedPrice;

                result.Summary.AddedCount++;
                result.Summary.Changes.Add(new PackagingPriceChange
                {
                    UnitName = unitName,
                    OldPrice = null,
                    NewPrice = calculatedPrice,
                    EffectiveBaseUnitQuantity = effectiveBaseUnits,
                    ChangeType = PriceChangeType.Added,
                    CalculationFormula = $"{activeBatch.SellingPrice:F2} (batch) × {effectiveBaseUnits:F2} (base units) = {calculatedPrice:F2}"
                });

                _logger.LogDebug(
                    "Added new packaging level '{UnitName}' with price {Price} for Shop {ShopId}, Drug {DrugId}",
                    unitName,
                    calculatedPrice,
                    inventory.ShopId,
                    inventory.DrugId);
            }
            else if (currentPrice == 0) // Treat 0 as null (needs auto-calculation)
            {
                // Auto-calculate from batch (was null/zero)
                var calculatedPrice = CalculatePrice(activeBatch.SellingPrice, effectiveBaseUnits);
                updatedPrices[unitName] = calculatedPrice;

                result.Summary.AutoCalculatedCount++;
                result.Summary.Changes.Add(new PackagingPriceChange
                {
                    UnitName = unitName,
                    OldPrice = 0,
                    NewPrice = calculatedPrice,
                    EffectiveBaseUnitQuantity = effectiveBaseUnits,
                    ChangeType = PriceChangeType.AutoCalculated,
                    CalculationFormula = $"{activeBatch.SellingPrice:F2} (batch) × {effectiveBaseUnits:F2} (base units) = {calculatedPrice:F2}"
                });

                _logger.LogDebug(
                    "Auto-calculated price for '{UnitName}': {Price} for Shop {ShopId}, Drug {DrugId}",
                    unitName,
                    calculatedPrice,
                    inventory.ShopId,
                    inventory.DrugId);
            }
            else
            {
                // Keep custom shop-defined price (non-null, non-zero)
                result.Summary.CustomPriceCount++;
                result.Summary.Changes.Add(new PackagingPriceChange
                {
                    UnitName = unitName,
                    OldPrice = currentPrice,
                    NewPrice = currentPrice,
                    EffectiveBaseUnitQuantity = effectiveBaseUnits,
                    ChangeType = PriceChangeType.CustomPriceKept,
                    CalculationFormula = "Custom shop price retained"
                });

                _logger.LogDebug(
                    "Keeping custom price for '{UnitName}': {Price} for Shop {ShopId}, Drug {DrugId}",
                    unitName,
                    currentPrice,
                    inventory.ShopId,
                    inventory.DrugId);
            }
        }

        // Update the inventory pricing
        inventory.ShopPricing.PackagingLevelPrices = updatedPrices;
        inventory.ShopPricing.LastPriceUpdate = DateTime.UtcNow;

        result.UpdatedPricing = inventory.ShopPricing;

        _logger.LogInformation(
            "Packaging price update complete for Shop {ShopId}, Drug {DrugId}. " +
            "Added: {Added}, Auto-calculated: {AutoCalc}, Custom: {Custom}, Total: {Total}",
            inventory.ShopId,
            inventory.DrugId,
            result.Summary.AddedCount,
            result.Summary.AutoCalculatedCount,
            result.Summary.CustomPriceCount,
            result.Summary.TotalLevels);

        return result;
    }

    /// <summary>
    /// Get the currently active batch (FIFO - oldest batch with stock)
    /// </summary>
    public Batch? GetActiveBatch(ShopInventory inventory)
    {
        if (inventory == null)
            throw new ArgumentNullException(nameof(inventory));

        var activeBatch = inventory.Batches
            .Where(b => b.Status == BatchStatus.Active && b.QuantityOnHand > 0)
            .OrderBy(b => b.ReceivedDate) // FIFO - First In, First Out
            .FirstOrDefault();

        if (activeBatch != null)
        {
            _logger.LogDebug(
                "Active batch for Shop {ShopId}, Drug {DrugId}: {BatchNumber} with {Quantity} units",
                inventory.ShopId,
                inventory.DrugId,
                activeBatch.BatchNumber,
                activeBatch.QuantityOnHand);
        }
        else
        {
            _logger.LogWarning(
                "No active batch found for Shop {ShopId}, Drug {DrugId}",
                inventory.ShopId,
                inventory.DrugId);
        }

        return activeBatch;
    }

    /// <summary>
    /// Calculate price for a packaging level
    /// Price = Batch Selling Price × Effective Base Unit Quantity
    /// </summary>
    /// <param name="batchSellingPrice">The selling price from the batch (per base unit)</param>
    /// <param name="effectiveBaseUnits">How many base units are in this packaging level</param>
    /// <returns>Calculated selling price for the packaging level</returns>
    private static decimal CalculatePrice(decimal batchSellingPrice, decimal effectiveBaseUnits)
    {
        // Round to 2 decimal places for currency
        return Math.Round(batchSellingPrice * effectiveBaseUnits, 2, MidpointRounding.AwayFromZero);
    }
}
