using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using pos_system_api.Core.Application.Categories.DTOs;
using pos_system_api.Core.Application.Categories.Queries.GetAllCategories;
using pos_system_api.Core.Application.Categories.Commands.CreateCategory;

namespace pos_system_api.API.Controllers;

/// <summary>
/// Drug categories management - for organizing drugs by type
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CategoriesController : BaseApiController
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all categories (with logo, name, ID)
    /// </summary>
    /// <param name="activeOnly">Only return active categories (default: true)</param>
    /// <returns>List of categories with logos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll([FromQuery] bool activeOnly = true)
    {
        var query = new GetAllCategoriesQuery(activeOnly);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <param name="dto">Category details</param>
    /// <returns>Created category</returns>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryDto dto)
    {
        try
        {
            var command = new CreateCategoryCommand(
                dto.Name,
                dto.LogoUrl,
                dto.Description,
                dto.ColorCode,
                dto.DisplayOrder
            );

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAll), new { activeOnly = true }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestWithDetails(ex);
        }
    }
}
