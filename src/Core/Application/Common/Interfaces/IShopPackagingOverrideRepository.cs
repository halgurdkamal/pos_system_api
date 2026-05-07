using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

public interface IShopPackagingOverrideRepository
{
    Task<IReadOnlyList<ShopPackagingOverride>> GetByShopAndDrugAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-fetch all overrides for a set of (shopId, drugId) pairs in a single query.
    /// Used to avoid N+1 patterns when assembling multi-drug projections.
    /// </summary>
    Task<IReadOnlyList<ShopPackagingOverride>> GetByShopAndDrugsAsync(
        string shopId,
        IReadOnlyCollection<string> drugIds,
        CancellationToken cancellationToken = default);

    Task<ShopPackagingOverride?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<ShopPackagingOverride> AddAsync(
        ShopPackagingOverride overrideEntity,
        CancellationToken cancellationToken = default);

    Task<ShopPackagingOverride> UpdateAsync(
        ShopPackagingOverride overrideEntity,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string id,
        CancellationToken cancellationToken = default);
}
