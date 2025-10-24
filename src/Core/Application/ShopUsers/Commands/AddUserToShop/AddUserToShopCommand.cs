using MediatR;
using pos_system_api.Core.Application.ShopUsers.DTOs;

namespace pos_system_api.Core.Application.ShopUsers.Commands.AddUserToShop;

/// <summary>
/// Command to add a user to a shop with specific role and permissions
/// </summary>
public record AddUserToShopCommand(
    string ShopId,
    string UserId,
    string Role,
    List<string>? CustomPermissions,
    bool IsOwner,
    string? Notes,
    string InvitedBy
) : IRequest<ShopMemberDto>;
