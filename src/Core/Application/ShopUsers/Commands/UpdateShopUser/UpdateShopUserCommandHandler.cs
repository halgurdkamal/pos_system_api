using MediatR;
using pos_system_api.Core.Application.Common.Exceptions;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.ShopUsers.DTOs;
using pos_system_api.Core.Domain.Auth.Enums;

namespace pos_system_api.Core.Application.ShopUsers.Commands.UpdateShopUser;

/// <summary>
/// Handler for updating user permissions/role in a shop
/// </summary>
public class UpdateShopUserCommandHandler : IRequestHandler<UpdateShopUserCommand, ShopMemberDto>
{
    private readonly IShopUserRepository _shopUserRepository;
    private readonly IUserRepository _userRepository;
    private readonly IShopRepository _shopRepository;
    private readonly ILogger<UpdateShopUserCommandHandler> _logger;

    public UpdateShopUserCommandHandler(
        IShopUserRepository shopUserRepository,
        IUserRepository userRepository,
        IShopRepository shopRepository,
        ILogger<UpdateShopUserCommandHandler> logger)
    {
        _shopUserRepository = shopUserRepository;
        _userRepository = userRepository;
        _shopRepository = shopRepository;
        _logger = logger;
    }

    public async Task<ShopMemberDto> Handle(UpdateShopUserCommand request, CancellationToken cancellationToken)
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

        // Get user and shop for DTO mapping
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);

        if (user == null || shop == null)
        {
            throw new NotFoundException("User or Shop not found");
        }

        // Update role if provided
        if (!string.IsNullOrEmpty(request.Role))
        {
            if (Enum.TryParse<ShopRole>(request.Role, out var shopRole))
            {
                shopUser.Role = shopRole;

                // If custom permissions not provided, use default permissions for role
                if (request.CustomPermissions == null || !request.CustomPermissions.Any())
                {
                    shopUser.SetRole(shopRole);
                }
            }
            else
            {
                throw new ArgumentException($"Invalid role: {request.Role}");
            }
        }

        // Update custom permissions if provided
        if (request.CustomPermissions != null)
        {
            var permissions = new List<Permission>();
            foreach (var permString in request.CustomPermissions)
            {
                if (Enum.TryParse<Permission>(permString, out var permission))
                {
                    permissions.Add(permission);
                }
                else
                {
                    _logger.LogWarning("Invalid permission '{Permission}' ignored", permString);
                }
            }
            shopUser.Permissions = permissions;
        }

        // Update IsOwner if provided
        if (request.IsOwner.HasValue)
        {
            // If removing owner status, check if there are other owners
            if (shopUser.IsOwner && !request.IsOwner.Value)
            {
                var owners = await _shopUserRepository.GetShopOwnersAsync(request.ShopId, cancellationToken);
                if (owners.Count <= 1)
                {
                    throw new InvalidOperationException("Cannot remove owner status from the last owner. Assign another owner first.");
                }
            }

            shopUser.IsOwner = request.IsOwner.Value;
        }

        // Update IsActive if provided
        if (request.IsActive.HasValue)
        {
            shopUser.IsActive = request.IsActive.Value;
        }

        // Update notes if provided
        if (request.Notes != null)
        {
            shopUser.Notes = request.Notes;
        }

        // Save changes
        var updated = await _shopUserRepository.UpdateAsync(shopUser, cancellationToken);

        _logger.LogInformation(
            "Updated permissions for user {UserId} in shop {ShopId}",
            request.UserId, request.ShopId);

        // Map to DTO
        return new ShopMemberDto
        {
            Id = updated.Id,
            UserId = updated.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            ShopId = updated.ShopId,
            ShopName = shop.ShopName,
            Role = updated.Role.ToString(),
            Permissions = updated.Permissions.Select(p => p.ToString()).ToList(),
            IsOwner = updated.IsOwner,
            IsActive = updated.IsActive,
            JoinedDate = updated.JoinedDate,
            InvitedBy = updated.InvitedBy,
            LastAccessDate = updated.LastAccessDate,
            Notes = updated.Notes
        };
    }
}
