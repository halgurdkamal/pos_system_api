using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Auth.Commands.Login;
using pos_system_api.Core.Application.Auth.Commands.RefreshToken;
using pos_system_api.Core.Application.Auth.Commands.Register;
using pos_system_api.Core.Application.Auth.DTOs;
using pos_system_api.Core.Application.Auth.Queries.GetCurrentUser;

namespace pos_system_api.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Register a new user.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequestDto request)
    {
        var command = new RegisterCommand(
            request.Username,
            request.Email,
            request.Password,
            request.FullName,
            request.ShopId,
            request.Phone
        );

        var user = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetCurrentUser), new { id = user.Id }, user);
    }

    /// <summary>Login with username, email, or phone number.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var response = await _mediator.Send(new LoginCommand(request.Identifier, request.Password));
        return Ok(response);
    }

    /// <summary>Refresh access token using refresh token.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var response = await _mediator.Send(new RefreshTokenCommand(request.AccessToken, request.RefreshToken));
        return Ok(response);
    }

    /// <summary>Get current authenticated user information.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var dto = await _mediator.Send(new GetCurrentUserQuery(User));
        return dto != null ? Ok(dto) : Unauthorized(new { error = "Invalid token" });
    }
}
