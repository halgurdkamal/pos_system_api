using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Core.Domain.Common;

namespace pos_system_api.Core.Domain.Shops.Entities;

/// <summary>
/// Many-to-many relationship between Users and Shops
/// Defines shop-specific roles and permissions for each user
/// </summary>
public class ShopUser : BaseEntity
{
    public ShopUser()
    {
        Id = $"SHOPUSER-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }

    /// <summary>
    /// User ID reference
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to User
    /// </summary>
    public Auth.Entities.User User { get; set; } = null!;

    /// <summary>
    /// Shop ID reference
    /// </summary>
    public string ShopId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to Shop
    /// </summary>
    public Shop Shop { get; set; } = null!;

    /// <summary>
    /// Shop-level role (predefined permission set)
    /// </summary>
    public ShopRole Role { get; set; } = ShopRole.Custom;

    /// <summary>
    /// Custom permissions for this user in this shop
    /// Stored as JSON array in database
    /// </summary>
    public List<Permission> Permissions { get; set; } = new();

    /// <summary>
    /// Date the user joined this shop
    /// </summary>
    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// ID of the user who invited this user to the shop
    /// Null if user is original owner
    /// </summary>
    public string? InvitedBy { get; set; }

    /// <summary>
    /// Is this user an owner of the shop?
    /// Shops can have multiple owners
    /// </summary>
    public bool IsOwner { get; set; } = false;

    /// <summary>
    /// Is this membership active?
    /// Set to false to revoke access without deleting record (audit trail)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last date user accessed this shop
    /// </summary>
    public DateTime? LastAccessDate { get; set; }

    /// <summary>
    /// Optional notes about this user's role/access
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Check if user has specific permission in this shop
    /// </summary>
    public bool HasPermission(Permission permission)
    {
        return IsActive && Permissions.Contains(permission);
    }

    /// <summary>
    /// Check if user has any of the specified permissions
    /// </summary>
    public bool HasAnyPermission(params Permission[] permissions)
    {
        return IsActive && permissions.Any(p => Permissions.Contains(p));
    }

    /// <summary>
    /// Check if user has all of the specified permissions
    /// </summary>
    public bool HasAllPermissions(params Permission[] permissions)
    {
        return IsActive && permissions.All(p => Permissions.Contains(p));
    }

    /// <summary>
    /// Add a permission to this user
    /// </summary>
    public void AddPermission(Permission permission)
    {
        if (!Permissions.Contains(permission))
        {
            Permissions.Add(permission);
        }
    }

    /// <summary>
    /// Remove a permission from this user
    /// </summary>
    public void RemovePermission(Permission permission)
    {
        Permissions.Remove(permission);
    }

    /// <summary>
    /// Set permissions based on a role
    /// </summary>
    public void SetRole(ShopRole role)
    {
        Role = role;
        Permissions = ShopRolePermissions.GetPermissionsForRole(role);
    }
}
