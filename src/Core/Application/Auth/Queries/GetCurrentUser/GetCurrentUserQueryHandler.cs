using System.Security.Claims;
using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;

namespace pos_system_api.Core.Application.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    public Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var principal = request.Principal;
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<UserDto?>(null);
        }

        var dto = new UserDto
        {
            Id = userId,
            Username = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            FullName = principal.FindFirst("fullName")?.Value ?? string.Empty,
            SystemRole = principal.FindFirst("systemRole")?.Value ?? "User",
            Shops = ParseShopsFromClaims(principal),
            IsActive = true,
            IsEmailVerified = false,
            LastLoginAt = null,
            Phone = null,
            ProfileImageUrl = null,
        };

        return Task.FromResult<UserDto?>(dto);
    }

    private static List<UserShopDto> ParseShopsFromClaims(ClaimsPrincipal principal)
    {
        var shopIds = principal.FindFirst("shopIds")?.Value;
        if (string.IsNullOrWhiteSpace(shopIds))
        {
            return new List<UserShopDto>();
        }

        return shopIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(shopId => BuildShopDto(principal, shopId))
            .ToList();
    }

    private static UserShopDto BuildShopDto(ClaimsPrincipal principal, string shopId)
    {
        var role = principal.FindFirst($"shop:{shopId}:role")?.Value ?? "Custom";
        var isOwner = principal.FindFirst($"shop:{shopId}:isOwner")?.Value == "True";
        var permissions = principal.Claims
            .Where(c => c.Type == $"shop:{shopId}:permission")
            .Select(c => c.Value)
            .ToList();

        return new UserShopDto
        {
            ShopId = shopId,
            ShopName = string.Empty, // Would need a DB lookup; preserve current behavior.
            Role = role,
            Permissions = permissions,
            IsOwner = isOwner,
            IsActive = true,
            JoinedDate = DateTime.UtcNow, // Would come from DB; preserve current behavior.
        };
    }
}
