using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Drugs.Commands.CreateDrug;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Application.Drugs.Queries.GetDrug;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugList;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugListEnhanced;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugDetail;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for drug operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DrugsController : BaseApiController
{
    private readonly IMediator _mediator;

    public DrugsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new drug in the catalog
    /// </summary>
    /// <param name="dto">Drug details including packaging information</param>
    /// <returns>The created drug</returns>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DrugDto>> CreateDrug([FromBody] CreateDrugDto dto)
    {
        try
        {
            var result = await _mediator.Send(new CreateDrugCommand(dto));
            return CreatedAtAction(nameof(GetDrug), new { id = result.DrugId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a single drug by ID
    /// </summary>
    /// <param name="id">Drug ID</param>
    /// <returns>Drug details</returns>
    [HttpGet("{id}")]
    [AllowAnonymous] // Drug catalog is public
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DrugDto>> GetDrug(string id)
    {
        var query = new GetDrugQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { error = $"Drug with ID '{id}' not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get list of drugs with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 20)</param>
    /// <returns>Paginated list of drugs</returns>
    [HttpGet]
    [AllowAnonymous] // Drug catalog is public
    [ProducesResponseType(typeof(PagedResult<DrugDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DrugDto>>> GetDrugs(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var query = new GetDrugListQuery(page, limit);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get enhanced drug list with images, prices, stock, and category info (LIGHTWEIGHT FOR BROWSING)
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 20)</param>
    /// <param name="searchTerm">Search by name, barcode, or manufacturer</param>
    /// <param name="category">Filter by category</param>
    /// <param name="inStock">Filter by stock availability</param>
    /// <returns>Paginated list with essential drug info</returns>
    [HttpGet("browse")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<DrugListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DrugListItemDto>>> GetDrugsBrowse(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? inStock = null)
    {
        var query = new GetDrugListEnhancedQuery(page, limit, searchTerm, category, inStock);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get FULL drug details including inventory across ALL shops (DETAILED VIEW)
    /// </summary>
    /// <param name="id">Drug ID</param>
    /// <returns>Complete drug information with shop inventories</returns>
    [HttpGet("{id}/detail")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DrugDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DrugDetailDto>> GetDrugDetail(string id)
    {
        var query = new GetDrugDetailQuery(id);
        var result = await _mediator.Send(query);

        if (result == null)
            return NotFound(new { error = $"Drug with ID '{id}' not found" });

        return Ok(result);
    }
}
