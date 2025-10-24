using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Core.Domain.Common;

namespace pos_system_api.Core.Domain.Auth.Entities;

/// <summary>
/// Represents a user in the system with authentication and authorization information
/// </summary>
public class User : BaseEntity
{
    // Basic Information
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    
    // System-Level Authorization (SuperAdmin vs User)
    public SystemRole SystemRole { get; set; } = SystemRole.User;
    
    // Shop Memberships (Many-to-Many relationship with Shops via ShopUser)
    public ICollection<Shops.Entities.ShopUser> ShopMemberships { get; set; } = new List<Shops.Entities.ShopUser>();
    
    // Status
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockedUntil { get; set; }
    
    // Security
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    // Optional Profile
    public string? Phone { get; set; }
    public string? ProfileImageUrl { get; set; }
    
    public User() { }

    public User(
        string username,
        string email,
        string passwordHash,
        string fullName,
        SystemRole systemRole = SystemRole.User)
    {
        Id = $"USER-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        SystemRole = systemRole;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateLastLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
        LastUpdated = DateTime.UtcNow;
    }

    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTime.UtcNow.AddMinutes(15); // Lock for 15 minutes
        }
        LastUpdated = DateTime.UtcNow;
    }

    public bool IsLocked() => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

    public void Unlock()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateRefreshToken(string refreshToken, DateTime expiryTime)
    {
        RefreshToken = refreshToken;
        RefreshTokenExpiryTime = expiryTime;
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Check if user is a SuperAdmin
    /// </summary>
    public bool IsSuperAdmin() => SystemRole == SystemRole.SuperAdmin;
    
    /// <summary>
    /// Check if user has access to a specific shop
    /// </summary>
    public bool HasAccessToShop(string shopId)
    {
        if (IsSuperAdmin()) return true; // SuperAdmins can access all shops
        return ShopMemberships.Any(sm => sm.ShopId == shopId && sm.IsActive);
    }
    
    /// <summary>
    /// Get user's membership for a specific shop
    /// </summary>
    public Shops.Entities.ShopUser? GetShopMembership(string shopId)
    {
        return ShopMemberships.FirstOrDefault(sm => sm.ShopId == shopId && sm.IsActive);
    }
    
    /// <summary>
    /// Check if user is an owner of a specific shop
    /// </summary>
    public bool IsOwnerOfShop(string shopId)
    {
        if (IsSuperAdmin()) return true;
        var membership = GetShopMembership(shopId);
        return membership?.IsOwner ?? false;
    }
}
