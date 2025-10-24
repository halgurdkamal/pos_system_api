namespace pos_system_api.Core.Application.Auth.DTOs;

public record TokenResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
}

public record UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string SystemRole { get; init; } = string.Empty; // "SuperAdmin" or "User"
    public List<UserShopDto> Shops { get; init; } = new(); // List of shops user has access to
    public bool IsActive { get; init; }
    public bool IsEmailVerified { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public string? Phone { get; init; }
    public string? ProfileImageUrl { get; init; }
}

public record UserShopDto
{
    public string ShopId { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty; // "Owner", "Manager", etc.
    public List<string> Permissions { get; init; } = new(); // List of permission names
    public bool IsOwner { get; init; }
    public bool IsActive { get; init; }
    public DateTime JoinedDate { get; init; }
}

public record LoginRequestDto
{
    /// <summary>
    /// Username, email, or phone number to login with
    /// </summary>
    public string Identifier { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RegisterRequestDto
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ShopId { get; init; }
    public string Role { get; init; } = "Staff"; // Default role
    public string? Phone { get; init; }
}

public record RefreshTokenRequestDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
