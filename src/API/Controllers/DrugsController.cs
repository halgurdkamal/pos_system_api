using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Application.Drugs.Queries.GetDrug;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugList;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for drug operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DrugsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DrugsController(IMediator mediator)
    {
        _mediator = mediator;
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
}
