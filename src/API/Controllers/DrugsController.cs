using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Drugs.Commands.CreateDrug;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Application.Drugs.Queries.GetDrug;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugDetail;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugList;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugListEnhanced;

namespace pos_system_api.API.Controllers;

/// <summary>API Controller for drug catalog operations.</summary>
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

    /// <summary>Create a new drug in the catalog.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DrugDto>> CreateDrug(
        [FromBody] CreateDrugDto dto, CancellationToken cancellationToken)
    {
        var createdBy = User?.Identity?.Name ?? "system";
        var result = await _mediator.Send(new CreateDrugCommand(dto, createdBy), cancellationToken);
        return CreatedAtAction(nameof(GetDrug), new { id = result.DrugId }, result);
    }

    /// <summary>Get a single drug by ID.</summary>
    [HttpGet("{id}")]
    [AllowAnonymous] // Drug catalog is public
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DrugDto>> GetDrug(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDrugQuery(id), cancellationToken);
        return result == null
            ? NotFound(new { error = $"Drug with ID '{id}' not found" })
            : Ok(result);
    }

    /// <summary>Get list of drugs with pagination.</summary>
    [HttpGet]
    [AllowAnonymous] // Drug catalog is public
    [ProducesResponseType(typeof(PagedResult<DrugDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DrugDto>>> GetDrugs(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var result = await _mediator.Send(new GetDrugListQuery(page, limit), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get enhanced drug list with images, prices, stock, and category info
    /// (lightweight, intended for catalog browsing).
    /// </summary>
    [HttpGet("browse")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<DrugListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DrugListItemDto>>> GetDrugsBrowse(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? inStock = null)
    {
        var result = await _mediator.Send(
            new GetDrugListEnhancedQuery(page, limit, searchTerm, category, inStock),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Get full drug details including inventory across all shops.</summary>
    [HttpGet("{id}/detail")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DrugDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DrugDetailDto>> GetDrugDetail(
        string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDrugDetailQuery(id), cancellationToken);
        return result == null
            ? NotFound(new { error = $"Drug with ID '{id}' not found" })
            : Ok(result);
    }
}
