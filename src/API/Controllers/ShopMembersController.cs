using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.ShopUsers.Commands.AddUserToShop;
using pos_system_api.Core.Application.ShopUsers.Commands.RemoveUserFromShop;
using pos_system_api.Core.Application.ShopUsers.Commands.UpdateShopUser;
using pos_system_api.Core.Application.ShopUsers.DTOs;
using pos_system_api.Core.Application.ShopUsers.Queries.GetShopMembers;
using pos_system_api.Core.Application.ShopUsers.Queries.GetUserMemberships;
using System.Security.Claims;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for managing shop user memberships and permissions
/// </summary>
[ApiController]
[Route("api/shops/{shopId}/members")]
[Produces("application/json")]
[Authorize]
public class ShopMembersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ShopMembersController> _logger;

    public ShopMembersController(IMediator mediator, ILogger<ShopMembersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all members of a shop
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="activeOnly">Only return active members (default: true)</param>
    /// <returns>List of shop members</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ShopMemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ShopMemberDto>>> GetShopMembers(
        string shopId, 
        [FromQuery] bool activeOnly = true)
    {
        try
        {
            var query = new GetShopMembersQuery(shopId, activeOnly);
            var members = await _mediator.Send(query);
            return Ok(members);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Add a user to a shop with specific role and permissions
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="dto">User and role information</param>
    /// <returns>Created shop member details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ShopMemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShopMemberDto>> AddUserToShop(
        string shopId, 
        [FromBody] AddUserToShopDto dto)
    {
        try
        {
            // Get current user ID from claims
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var command = new AddUserToShopCommand(
                shopId,
                dto.UserId,
                dto.Role,
                dto.CustomPermissions,
                dto.IsOwner,
                dto.Notes,
                currentUserId
            );

            var member = await _mediator.Send(command);

            _logger.LogInformation(
                "User {CurrentUserId} added user {UserId} to shop {ShopId}",
                currentUserId, dto.UserId, shopId);

            return CreatedAtAction(
                nameof(GetShopMembers), 
                new { shopId }, 
                member);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Update user permissions/role in a shop
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="dto">Updated role and permissions</param>
    /// <returns>Updated shop member details</returns>
    [HttpPut("{userId}")]
    [ProducesResponseType(typeof(ShopMemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ShopMemberDto>> UpdateShopUser(
        string shopId, 
        string userId, 
        [FromBody] UpdateShopUserDto dto)
    {
        try
        {
            var command = new UpdateShopUserCommand(
                shopId,
                userId,
                dto.Role,
                dto.CustomPermissions,
                dto.IsOwner,
                dto.IsActive,
                dto.Notes
            );

            var member = await _mediator.Send(command);

            _logger.LogInformation(
                "Updated permissions for user {UserId} in shop {ShopId}",
                userId, shopId);

            return Ok(member);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Remove a user from a shop
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="userId">User ID to remove</param>
    /// <returns>Success status</returns>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveUserFromShop(string shopId, string userId)
    {
        try
        {
            var command = new RemoveUserFromShopCommand(shopId, userId);
            await _mediator.Send(command);

            _logger.LogInformation(
                "Removed user {UserId} from shop {ShopId}",
                userId, shopId);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    /// <summary>
    /// Get all shop memberships for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="activeOnly">Only return active memberships (default: true)</param>
    /// <returns>List of user's shop memberships</returns>
    [HttpGet("/api/users/{userId}/shops")]
    [ProducesResponseType(typeof(List<ShopMemberDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ShopMemberDto>>> GetUserMemberships(
        string userId, 
        [FromQuery] bool activeOnly = true)
    {
        var query = new GetUserMembershipsQuery(userId, activeOnly);
        var memberships = await _mediator.Send(query);
        return Ok(memberships);
    }
}
