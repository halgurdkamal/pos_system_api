using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Inventory.Commands.CreateStockAdjustment;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Queries.GetAdjustmentHistory;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for stock adjustments and audit trail
/// </summary>
[ApiController]
[Route("api/stock-adjustments")]
[Produces("application/json")]
[Authorize(Policy = "ShopAccess")]
public class StockAdjustmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockAdjustmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a stock adjustment record (manual adjustment, damage, theft, etc.)
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="dto">Adjustment details</param>
    /// <returns>Created adjustment record</returns>
    [HttpPost("shops/{shopId}")]
    [ProducesResponseType(typeof(StockAdjustmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockAdjustmentDto>> CreateAdjustment(
        string shopId,
        [FromBody] CreateStockAdjustmentDto dto)
    {
        try
        {
            // TODO: Get authenticated user ID from claims
            var userId = User.FindFirst("sub")?.Value ?? "System";

            var command = new CreateStockAdjustmentCommand(
                shopId,
                dto.DrugId,
                dto.BatchNumber,
                dto.AdjustmentType,
                dto.QuantityChanged,
                dto.Reason,
                userId,
                dto.Notes,
                dto.ReferenceId,
                dto.ReferenceType
            );

            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetAdjustmentHistory), new { shopId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get stock adjustment history for a shop
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="drugId">Optional: Filter by drug ID</param>
    /// <param name="startDate">Optional: Start date filter</param>
    /// <param name="endDate">Optional: End date filter</param>
    /// <param name="adjustmentType">Optional: Filter by adjustment type (Sale, Return, Damage, etc.)</param>
    /// <param name="limit">Optional: Maximum number of records (default: 100)</param>
    /// <returns>List of adjustment records</returns>
    [HttpGet("shops/{shopId}")]
    [ProducesResponseType(typeof(IEnumerable<StockAdjustmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StockAdjustmentDto>>> GetAdjustmentHistory(
        string shopId,
        [FromQuery] string? drugId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? adjustmentType = null,
        [FromQuery] int? limit = 100)
    {
        var query = new GetAdjustmentHistoryQuery(
            shopId,
            drugId,
            startDate,
            endDate,
            adjustmentType,
            limit
        );

        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Get adjustment history for a specific drug in a shop
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <param name="drugId">Drug ID</param>
    /// <param name="startDate">Optional: Start date filter</param>
    /// <param name="endDate">Optional: End date filter</param>
    /// <returns>List of adjustment records for the drug</returns>
    [HttpGet("shops/{shopId}/drugs/{drugId}")]
    [ProducesResponseType(typeof(IEnumerable<StockAdjustmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StockAdjustmentDto>>> GetDrugAdjustmentHistory(
        string shopId,
        string drugId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new GetAdjustmentHistoryQuery(
            shopId,
            drugId,
            startDate,
            endDate
        );

        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
