using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Shops.Commands.RegisterShop;
using pos_system_api.Core.Application.Shops.Commands.UpdateShop;
using pos_system_api.Core.Application.Shops.Commands.CreateOwnShop;
using pos_system_api.Core.Application.Shops.DTOs;
using pos_system_api.Core.Application.Shops.Queries.GetAllShops;
using pos_system_api.Core.Application.Shops.Queries.GetShopById;
using pos_system_api.Core.Application.Shops.Queries.SearchShops;
using pos_system_api.Core.Domain.Shops.Entities;
using System.Security.Claims;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for shop operations (Multi-tenant pharmacy shops)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize] // All endpoints require authentication
public class ShopsController : BaseApiController
{
    private readonly IMediator _mediator;

    public ShopsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create your own pharmacy shop (for regular users)
    /// User becomes the owner of the created shop with full permissions
    /// </summary>
    /// <param name="request">Shop creation details</param>
    /// <returns>Created shop details</returns>
    [HttpPost("create-own")]
    [Authorize] // Any authenticated user can create their own shop
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ShopDto>> CreateOwnShop([FromBody] CreateOwnShopRequestDto request)
    {
        try
        {
            // Get user ID from JWT claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { error = "User not authenticated" });
            }

            var command = new CreateOwnShopCommand(
                userId,
                request.ShopName,
                request.PhoneNumber,
                request.Email,
                request.Address,
                request.City,
                request.State,
                request.Country,
                request.PostalCode,
                request.LicenseNumber,
                request.TaxId,
                request.Description
            );

            var result = await _mediator.Send(command);
            
            return CreatedAtAction(nameof(GetShop), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Register a new pharmacy shop (Admin only)
    /// </summary>
    /// <param name="dto">Shop registration details</param>
    /// <returns>Created shop details</returns>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")] // Only admins can register new shops
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShopDto>> RegisterShop([FromBody] CreateShopDto dto)
    {
        try
        {
            var command = new RegisterShopCommand(dto);
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetShop), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all shops with optional filters and pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 20)</param>
    /// <param name="status">Filter by shop status (optional)</param>
    /// <returns>Paginated list of shops</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ShopDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ShopDto>>> GetShops(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] ShopStatus? status = null)
    {
        var query = new GetAllShopsQuery(page, limit, status);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get a single shop by ID
    /// </summary>
    /// <param name="id">Shop ID</param>
    /// <returns>Shop details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopDto>> GetShop(string id)
    {
        var query = new GetShopByIdQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { error = $"Shop with ID '{id}' not found" });

        return Ok(result);
    }

    /// <summary>
    /// Search shops by name
    /// </summary>
    /// <param name="term">Search term</param>
    /// <returns>List of matching shops</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ShopDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ShopDto>>> SearchShops([FromQuery] string term)
    {
        var query = new SearchShopsQuery(term);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Update shop details
    /// </summary>
    /// <param name="id">Shop ID</param>
    /// <param name="dto">Updated shop details</param>
    /// <returns>Updated shop details</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "ShopOwnerOrAdmin")] // Shop owners can update their shop, admins can update any
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ShopDto>> UpdateShop(string id, [FromBody] UpdateShopDto dto)
    {
        try
        {
            var command = new UpdateShopCommand(id, dto);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update shop receipt configuration
    /// </summary>
    /// <param name="id">Shop ID</param>
    /// <param name="dto">Updated receipt configuration</param>
    /// <returns>Updated shop details</returns>
    [HttpPut("{id}/receipt-config")]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopDto>> UpdateReceiptConfig(string id, [FromBody] UpdateReceiptConfigDto dto)
    {
        try
        {
            var command = new UpdateReceiptConfigCommand(id, dto);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update shop hardware configuration
    /// </summary>
    /// <param name="id">Shop ID</param>
    /// <param name="dto">Updated hardware configuration</param>
    /// <returns>Updated shop details</returns>
    [HttpPut("{id}/hardware-config")]
    [ProducesResponseType(typeof(ShopDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ShopDto>> UpdateHardwareConfig(string id, [FromBody] UpdateHardwareConfigDto dto)
    {
        try
        {
            var command = new UpdateHardwareConfigCommand(id, dto);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
