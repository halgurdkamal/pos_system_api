using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Core.Application.Inventory.Services;

/// <summary>
/// Service for managing automatic pricing updates for packaging levels from active batches
/// </summary>
public interface IPackagingPricingService
{
    /// <summary>
    /// Update packaging level prices from the active batch
    /// - For entries with null values: auto-calculate from batch selling price × effective base unit quantity
    /// - For entries with numeric values: leave unchanged (shop override)
    /// - For missing packaging levels: add them with null (auto-calculate)
    /// </summary>
    /// <param name="inventory">The shop inventory to update</param>
    /// <param name="activeBatch">The current active batch (used for price calculation)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing updated pricing and summary of changes</returns>
    Task<PackagingPricingUpdateResult> UpdatePackagingPricesFromBatchAsync(
        ShopInventory inventory,
        Batch activeBatch,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the currently active batch (oldest batch with quantity on hand)
    /// </summary>
    /// <param name="inventory">The shop inventory</param>
    /// <returns>The active batch or null if no stock available</returns>
    Batch? GetActiveBatch(ShopInventory inventory);
}
