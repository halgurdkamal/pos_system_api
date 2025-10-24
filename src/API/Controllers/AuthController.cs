using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Auth.Commands.Login;
using pos_system_api.Core.Application.Auth.Commands.Register;
using pos_system_api.Core.Application.Auth.Commands.RefreshToken;
using pos_system_api.Core.Application.Auth.DTOs;
using System.Security.Claims;

namespace pos_system_api.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var command = new RegisterCommand(
                request.Username,
                request.Email,
                request.Password,
                request.FullName,
                request.ShopId,
                request.Role,
                request.Phone
            );

            var user = await _mediator.Send(command);

            _logger.LogInformation("User {Username} registered successfully", user.Username);

            return CreatedAtAction(nameof(GetCurrentUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid registration request: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Login with username, email, or phone number
    /// </summary>
    /// <param name="request">Login credentials (identifier can be username, email, or phone)</param>
    /// <returns>JWT tokens and user information with available shops and roles</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var command = new LoginCommand(request.Identifier, request.Password);
            var response = await _mediator.Send(command);

            _logger.LogInformation("User {Identifier} logged in successfully", request.Identifier);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Login failed for user {Identifier}: {Message}", request.Identifier, ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);
            var response = await _mediator.Send(command);

            _logger.LogInformation("Token refreshed successfully for user {UserId}", response.User.Id);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get current authenticated user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserDto> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var fullName = User.FindFirst("fullName")?.Value;
        var shopIds = User.FindFirst("shopIds")?.Value;
        var systemRole = User.FindFirst("systemRole")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "Invalid token" });
        }

        // Parse shops from token claims (simplified - full implementation would query database)
        var shops = new List<UserShopDto>();
        if (!string.IsNullOrWhiteSpace(shopIds))
        {
            var shopIdList = shopIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var sid in shopIdList)
            {
                // Get shop-specific role and permissions from claims
                var shopRole = User.FindFirst($"shop:{sid}:role")?.Value ?? "Custom";
                var isOwner = User.FindFirst($"shop:{sid}:isOwner")?.Value == "True";
                var permissions = User.Claims
                    .Where(c => c.Type == $"shop:{sid}:permission")
                    .Select(c => c.Value)
                    .ToList();

                shops.Add(new UserShopDto
                {
                    ShopId = sid,
                    ShopName = "", // Would need to query database for shop name
                    Role = shopRole,
                    Permissions = permissions,
                    IsOwner = isOwner,
                    IsActive = true,
                    JoinedDate = DateTime.UtcNow // Would come from database
                });
            }
        }

        var userDto = new UserDto
        {
            Id = userId,
            Username = username ?? "",
            Email = email ?? "",
            FullName = fullName ?? "",
            SystemRole = systemRole ?? "User",
            Shops = shops,
            IsActive = true,
            IsEmailVerified = false,
            LastLoginAt = null,
            Phone = null,
            ProfileImageUrl = null
        };

        return Ok(userDto);
    }
}
