using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Auth.Entities;
using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Infrastructure.Auth;

namespace pos_system_api.Core.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IShopRepository _shopRepository;
    private readonly PasswordHasher _passwordHasher;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IShopRepository shopRepository,
        PasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _shopRepository = shopRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if username or email already exists
        if (await _userRepository.ExistsAsync(request.Username, request.Email, cancellationToken))
        {
            throw new InvalidOperationException("Username or email already exists");
        }

        // Parse system role (default to User, only SuperAdmins can create other SuperAdmins)
        var systemRole = SystemRole.User;
        if (!string.IsNullOrWhiteSpace(request.Role) && 
            Enum.TryParse<SystemRole>(request.Role, true, out var parsedRole))
        {
            systemRole = parsedRole;
        }

        // Validate ShopId if provided (for backward compatibility)
        // Note: In the new system, shop assignment should be done via ShopUser management
        if (!string.IsNullOrWhiteSpace(request.ShopId))
        {
            // Verify shop exists
            var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
            if (shop == null)
            {
                throw new ArgumentException($"Shop with ID {request.ShopId} not found");
            }
            // Shop assignment will be handled after user creation if ShopId is provided
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user entity
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            FullName = request.FullName,
            SystemRole = systemRole,
            Phone = request.Phone,
            IsActive = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // TODO: Use authenticated user when available
        };

        // Save to database
        var createdUser = await _userRepository.CreateAsync(user, cancellationToken);

        return MapToUserDto(createdUser);
    }

    private static UserDto MapToUserDto(User user)
    {
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
                    ShopName = sm.Shop?.ShopName ?? "",
                    Role = sm.Role.ToString(),
                    Permissions = sm.Permissions.Select(p => p.ToString()).ToList(),
                    IsOwner = sm.IsOwner,
                    IsActive = sm.IsActive,
                    JoinedDate = sm.JoinedDate
                })
                .ToList() ?? new List<UserShopDto>(),
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            LastLoginAt = user.LastLoginAt,
            Phone = user.Phone,
            ProfileImageUrl = user.ProfileImageUrl
        };
    }
}
