using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using pos_system_api.Core.Domain.Auth.Entities;

namespace pos_system_api.Infrastructure.Auth;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.SystemRole.ToString()),
            new Claim("fullName", user.FullName),
            new Claim("systemRole", user.SystemRole.ToString())
        };

        // Add shop memberships as claims (for multi-shop access)
        if (user.ShopMemberships != null && user.ShopMemberships.Any())
        {
            var activeShops = user.ShopMemberships.Where(sm => sm.IsActive).ToList();
            
            // Add shop IDs as comma-separated claim
            if (activeShops.Any())
            {
                var shopIds = string.Join(",", activeShops.Select(sm => sm.ShopId));
                claims.Add(new Claim("shopIds", shopIds));
                
                // Add shop-specific permissions
                foreach (var shopMembership in activeShops)
                {
                    claims.Add(new Claim($"shop:{shopMembership.ShopId}:role", shopMembership.Role.ToString()));
                    claims.Add(new Claim($"shop:{shopMembership.ShopId}:isOwner", shopMembership.IsOwner.ToString()));
                    
                    // Add permissions for this shop
                    foreach (var permission in shopMembership.Permissions)
                    {
                        claims.Add(new Claim($"shop:{shopMembership.ShopId}:permission", permission.ToString()));
                    }
                }
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured")));
        
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT Secret Key not configured"))),
            ValidateLifetime = false // Don't validate expiration for refresh
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}
