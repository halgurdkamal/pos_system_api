using MediatR;
using pos_system_api.Core.Application.Common.Exceptions;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.ShopUsers.DTOs;
using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Core.Domain.Shops.Entities;

namespace pos_system_api.Core.Application.ShopUsers.Commands.AddUserToShop;

/// <summary>
/// Handler for adding a user to a shop
/// </summary>
public class AddUserToShopCommandHandler : IRequestHandler<AddUserToShopCommand, ShopMemberDto>
{
    private readonly IShopUserRepository _shopUserRepository;
    private readonly IUserRepository _userRepository;
    private readonly IShopRepository _shopRepository;
    private readonly ILogger<AddUserToShopCommandHandler> _logger;

    public AddUserToShopCommandHandler(
        IShopUserRepository shopUserRepository,
        IUserRepository userRepository,
        IShopRepository shopRepository,
        ILogger<AddUserToShopCommandHandler> logger)
    {
        _shopUserRepository = shopUserRepository;
        _userRepository = userRepository;
        _shopRepository = shopRepository;
        _logger = logger;
    }

    public async Task<ShopMemberDto> Handle(AddUserToShopCommand request, CancellationToken cancellationToken)
    {
        // Validate shop exists
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
        if (shop == null)
        {
            throw new NotFoundException($"Shop with ID '{request.ShopId}' not found");
        }

        // Validate user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException($"User with ID '{request.UserId}' not found");
        }

        // Check if user is already a member
        var existingMembership = await _shopUserRepository.GetByUserAndShopAsync(
            request.UserId, 
            request.ShopId, 
            cancellationToken);
        
        if (existingMembership != null)
        {
            throw new InvalidOperationException($"User '{user.Username}' is already a member of shop '{shop.ShopName}'");
        }

        // Parse role
        if (!Enum.TryParse<ShopRole>(request.Role, out var shopRole))
        {
            throw new ArgumentException($"Invalid role: {request.Role}. Valid roles: Owner, Manager, Cashier, InventoryClerk, Viewer, Custom");
        }

        // Create ShopUser entity
        var shopUser = new ShopUser
        {
            UserId = request.UserId,
            ShopId = request.ShopId,
            Role = shopRole,
            IsOwner = request.IsOwner,
            IsActive = true,
            JoinedDate = DateTime.UtcNow,
            InvitedBy = request.InvitedBy,
            Notes = request.Notes
        };

        // Set permissions based on role or custom permissions
        if (request.CustomPermissions != null && request.CustomPermissions.Any())
        {
            // Custom permissions provided
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
        else
        {
            // Use default permissions for the role
            shopUser.SetRole(shopRole);
        }

        // Save to database
        var created = await _shopUserRepository.CreateAsync(shopUser, cancellationToken);

        _logger.LogInformation(
            "User {UserId} ({Username}) added to shop {ShopId} ({ShopName}) with role {Role}",
            user.Id, user.Username, shop.Id, shop.ShopName, shopRole);

        // Map to DTO
        return new ShopMemberDto
        {
            Id = created.Id,
            UserId = created.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            ShopId = created.ShopId,
            ShopName = shop.ShopName,
            Role = created.Role.ToString(),
            Permissions = created.Permissions.Select(p => p.ToString()).ToList(),
            IsOwner = created.IsOwner,
            IsActive = created.IsActive,
            JoinedDate = created.JoinedDate,
            InvitedBy = created.InvitedBy,
            LastAccessDate = created.LastAccessDate,
            Notes = created.Notes
        };
    }
}
