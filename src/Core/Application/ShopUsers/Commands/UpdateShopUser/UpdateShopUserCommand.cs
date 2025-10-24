using MediatR;
using pos_system_api.Core.Application.ShopUsers.DTOs;

namespace pos_system_api.Core.Application.ShopUsers.Commands.UpdateShopUser;

/// <summary>
/// Command to update user permissions/role in a shop
/// </summary>
public record UpdateShopUserCommand(
    string ShopId,
    string UserId,
    string? Role,
    List<string>? CustomPermissions,
    bool? IsOwner,
    bool? IsActive,
    string? Notes
) : IRequest<ShopMemberDto>;
