using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for ShopInventory entity operations
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Get inventory by ID
    /// </summary>
    Task<ShopInventory?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get inventory for a specific shop and drug
    /// </summary>
    Task<ShopInventory?> GetByShopAndDrugAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Same as <see cref="GetByShopAndDrugAsync"/> but acquires a row-level lock
    /// (<c>SELECT ... FOR UPDATE</c>) on the matching <c>ShopInventory</c> row.
    /// MUST be called inside an <see cref="IUnitOfWork.BeginTransactionAsync"/>
    /// transaction; the lock is released when the transaction commits or rolls back.
    /// Returns a tracked entity so subsequent <see cref="UpdateAsync"/> works.
    /// </summary>
    Task<ShopInventory?> GetByShopAndDrugForUpdateAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-fetch inventory rows for a single shop and a set of drug IDs in one query.
    /// Used to avoid N+1 patterns. Missing (shopId, drugId) pairs are silently skipped.
    /// </summary>
    Task<IReadOnlyList<ShopInventory>> GetByShopAndDrugsAsync(
        string shopId,
        IReadOnlyCollection<string> drugIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all inventory items for a shop with pagination
    /// </summary>
    Task<(IEnumerable<ShopInventory> Items, int TotalCount)> GetByShopAsync(
        string shopId,
        int page,
        int limit,
        bool? isAvailable = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all inventory items for a shop without pagination
    /// </summary>
    Task<IEnumerable<ShopInventory>> GetAllByShopAsync(
        string shopId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get low stock items for a shop (below reorder point)
    /// </summary>
    Task<IEnumerable<ShopInventory>> GetLowStockAsync(
        string shopId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get items with batches expiring within specified days
    /// </summary>
    Task<IEnumerable<ShopInventory>> GetExpiringBatchesAsync(
        string shopId,
        int days,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get out of stock items for a shop
    /// </summary>
    Task<IEnumerable<ShopInventory>> GetOutOfStockAsync(
        string shopId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search inventory by drug name
    /// </summary>
    Task<IEnumerable<ShopInventory>> SearchByDrugNameAsync(
        string shopId,
        string searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new inventory item
    /// </summary>
    Task<ShopInventory> AddAsync(ShopInventory inventory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing inventory item
    /// </summary>
    Task<ShopInventory> UpdateAsync(ShopInventory inventory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete inventory item
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if shop already has inventory for a drug
    /// </summary>
    Task<bool> ExistsAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get total stock value for a shop
    /// </summary>
    Task<decimal> GetTotalStockValueAsync(
        string shopId,
        CancellationToken cancellationToken = default);
}
