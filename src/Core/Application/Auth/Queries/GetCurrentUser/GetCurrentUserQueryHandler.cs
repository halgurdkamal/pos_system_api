using System.Security.Claims;
using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;

namespace pos_system_api.Core.Application.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var userId = request.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            SystemRole = user.SystemRole.ToString(),
            Shops = user.ShopMemberships?
                .Where(sm => sm.IsActive)
                .Select(sm => new UserShopDto
                {
                    ShopId = sm.ShopId,
                    ShopName = sm.Shop?.ShopName ?? string.Empty,
                    Role = sm.Role.ToString(),
                    Permissions = sm.Permissions.Select(p => p.ToString()).ToList(),
                    IsOwner = sm.IsOwner,
                    IsActive = sm.IsActive,
                    JoinedDate = sm.JoinedDate,
                })
                .ToList() ?? new List<UserShopDto>(),
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            LastLoginAt = user.LastLoginAt,
            Phone = user.Phone,
            ProfileImageUrl = user.ProfileImageUrl,
        };
    }
}
