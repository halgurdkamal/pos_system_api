using pos_system_api.Core.Domain.Auth.Enums;

namespace pos_system_api.Core.Application.ShopUsers.DTOs;

/// <summary>
/// DTO for shop member information
/// </summary>
public class ShopMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string ShopId { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public bool IsOwner { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedDate { get; set; }
    public string? InvitedBy { get; set; }
    public DateTime? LastAccessDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for adding a user to a shop
/// </summary>
public class AddUserToShopDto
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier"; // Default role
    public List<string>? CustomPermissions { get; set; }
    public bool IsOwner { get; set; } = false;
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating user permissions in a shop
/// </summary>
public class UpdateShopUserDto
{
    public string Role { get; set; } = string.Empty;
    public List<string>? CustomPermissions { get; set; }
    public bool? IsOwner { get; set; }
    public bool? IsActive { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for inviting user via email
/// </summary>
public class InviteUserToShopDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier";
    public List<string>? CustomPermissions { get; set; }
    public bool IsOwner { get; set; } = false;
    public string? Notes { get; set; }
}
