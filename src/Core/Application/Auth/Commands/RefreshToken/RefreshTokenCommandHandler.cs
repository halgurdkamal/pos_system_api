using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace pos_system_api.Core.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        JwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<TokenResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Validate expired access token and get claims
        var principal = _jwtTokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            throw new UnauthorizedAccessException("Invalid access token");
        }

        // Get user ID from claims
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            throw new UnauthorizedAccessException("Invalid token claims");
        }

        // Get user from database
        var user = await _userRepository.GetByIdAsync(userIdClaim, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // Validate refresh token
        if (user.RefreshToken != request.RefreshToken)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        // Check if refresh token is expired
        if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token has expired");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive");
        }

        // Generate new tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        // Get refresh token expiry from config
        var refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

        // Update user with new refresh token
        user.UpdateRefreshToken(newRefreshToken, refreshTokenExpiry);
        user.LastUpdated = DateTime.UtcNow;
        user.UpdatedBy = user.Username; // Updated by self
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Get access token expiry from config
        var accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes);

        return new TokenResponseDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
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
