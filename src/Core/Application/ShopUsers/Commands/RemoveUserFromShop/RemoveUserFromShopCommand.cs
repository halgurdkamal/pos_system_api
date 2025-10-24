using MediatR;

namespace pos_system_api.Core.Application.ShopUsers.Commands.RemoveUserFromShop;

/// <summary>
/// Command to remove a user from a shop
/// </summary>
public record RemoveUserFromShopCommand(
    string ShopId,
    string UserId
) : IRequest<bool>;
