using pos_system_api.Core.Domain.Shops.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Shop entity operations
/// </summary>
public interface IShopRepository
{
    /// <summary>
    /// Get a shop by its ID
    /// </summary>
    Task<Shop?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a shop by license number
    /// </summary>
    Task<Shop?> GetByLicenseNumberAsync(string licenseNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all shops with pagination
    /// </summary>
    Task<(IEnumerable<Shop> Shops, int TotalCount)> GetAllAsync(
        int page,
        int limit,
        ShopStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search shops by name
    /// </summary>
    Task<IEnumerable<Shop>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new shop
    /// </summary>
    Task<Shop> AddAsync(Shop shop, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing shop
    /// </summary>
    Task<Shop> UpdateAsync(Shop shop, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a shop
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a license number already exists
    /// </summary>
    Task<bool> LicenseNumberExistsAsync(
        string licenseNumber,
        string? excludeShopId = null,
        CancellationToken cancellationToken = default);
}
