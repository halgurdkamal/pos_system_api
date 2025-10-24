using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;

namespace pos_system_api.Core.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(
        IUserRepository userRepository,
        PasswordHasher passwordHasher,
        JwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<TokenResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Get user by identifier (username, email, or phone)
        var user = await _userRepository.GetByIdentifierAsync(request.Identifier, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Check if account is locked
        if (user.IsLocked())
        {
            throw new UnauthorizedAccessException($"Account is locked until {user.LockedUntil:yyyy-MM-dd HH:mm:ss}");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Record failed login attempt
            user.RecordFailedLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Update last login
        user.UpdateLastLogin();

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Get refresh token expiry from config
        var refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

        // Update user with refresh token
        user.UpdateRefreshToken(refreshToken, refreshTokenExpiry);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Get access token expiry from config
        var accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = accessTokenExpiry,
            User = MapToUserDto(user)
        };
    }

    private static UserDto MapToUserDto(Core.Domain.Auth.Entities.User user)
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
