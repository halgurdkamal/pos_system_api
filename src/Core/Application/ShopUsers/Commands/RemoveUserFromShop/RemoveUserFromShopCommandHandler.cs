using MediatR;
using pos_system_api.Core.Application.Common.Exceptions;
using pos_system_api.Core.Application.Common.Interfaces;

namespace pos_system_api.Core.Application.ShopUsers.Commands.RemoveUserFromShop;

/// <summary>
/// Handler for removing a user from a shop
/// </summary>
public class RemoveUserFromShopCommandHandler : IRequestHandler<RemoveUserFromShopCommand, bool>
{
    private readonly IShopUserRepository _shopUserRepository;
    private readonly ILogger<RemoveUserFromShopCommandHandler> _logger;

    public RemoveUserFromShopCommandHandler(
        IShopUserRepository shopUserRepository,
        ILogger<RemoveUserFromShopCommandHandler> logger)
    {
        _shopUserRepository = shopUserRepository;
        _logger = logger;
    }

    public async Task<bool> Handle(RemoveUserFromShopCommand request, CancellationToken cancellationToken)
    {
        // Get the shop user relationship
        var shopUser = await _shopUserRepository.GetByUserAndShopAsync(
            request.UserId, 
            request.ShopId, 
            cancellationToken);

        if (shopUser == null)
        {
            throw new NotFoundException($"User is not a member of the specified shop");
        }

        // Check if this is the last owner
        if (shopUser.IsOwner)
        {
            var owners = await _shopUserRepository.GetShopOwnersAsync(request.ShopId, cancellationToken);
            if (owners.Count <= 1)
            {
                throw new InvalidOperationException("Cannot remove the last owner of a shop. Transfer ownership first.");
            }
        }

        // Deactivate instead of delete (soft delete for audit trail)
        await _shopUserRepository.DeactivateAsync(shopUser.Id, cancellationToken);

        _logger.LogInformation(
            "User {UserId} removed from shop {ShopId}",
            request.UserId, request.ShopId);

        return true;
    }
}
