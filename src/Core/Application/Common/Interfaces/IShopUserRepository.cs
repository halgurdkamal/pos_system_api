using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Core.Domain.Shops.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for ShopUser entity
/// </summary>
public interface IShopUserRepository
{
    /// <summary>
    /// Get shop user by ID
    /// </summary>
    Task<ShopUser?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get shop user by user ID and shop ID
    /// </summary>
    Task<ShopUser?> GetByUserAndShopAsync(string userId, string shopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active members of a shop
    /// </summary>
    Task<List<ShopUser>> GetShopMembersAsync(string shopId, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all shops a user is a member of
    /// </summary>
    Task<List<ShopUser>> GetUserShopsAsync(string userId, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all owners of a shop
    /// </summary>
    Task<List<ShopUser>> GetShopOwnersAsync(string shopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has specific permission in a shop
    /// </summary>
    Task<bool> HasPermissionAsync(string userId, string shopId, Permission permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user is an owner of a shop
    /// </summary>
    Task<bool> IsShopOwnerAsync(string userId, string shopId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create shop user (add user to shop)
    /// </summary>
    Task<ShopUser> CreateAsync(ShopUser shopUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update shop user (update role/permissions)
    /// </summary>
    Task<ShopUser> UpdateAsync(ShopUser shopUser, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete shop user (remove user from shop)
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate shop user (soft delete - keep audit trail)
    /// </summary>
    Task DeactivateAsync(string id, CancellationToken cancellationToken = default);
}
