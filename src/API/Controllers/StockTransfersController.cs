using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Inventory.Commands.StockTransfer;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Queries.StockTransfer;

namespace pos_system_api.API.Controllers;

[ApiController]
[Route("api/stock-transfers")]
[Produces("application/json")]
[Authorize(Policy = "ShopAccess")]
public class StockTransfersController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockTransfersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("shops/{fromShopId}")]
    [ProducesResponseType(typeof(StockTransferDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<StockTransferDto>> CreateTransfer(
        string fromShopId,
        [FromBody] CreateTransferDto dto)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "System";
            var command = new CreateTransferCommand(
                fromShopId, dto.ToShopId, dto.DrugId, dto.BatchNumber,
                dto.Quantity, userId, dto.Notes);
            
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetPendingTransfers), new { shopId = fromShopId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{transferId}/approve")]
    [ProducesResponseType(typeof(StockTransferDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockTransferDto>> ApproveTransfer(string transferId)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "System";
            var result = await _mediator.Send(new ApproveTransferCommand(transferId, userId));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{transferId}/receive")]
    [ProducesResponseType(typeof(StockTransferDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockTransferDto>> ReceiveTransfer(string transferId)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "System";
            var result = await _mediator.Send(new ReceiveTransferCommand(transferId, userId));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{transferId}/cancel")]
    [ProducesResponseType(typeof(StockTransferDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockTransferDto>> CancelTransfer(
        string transferId,
        [FromBody] CancelTransferDto dto)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "System";
            var result = await _mediator.Send(new CancelTransferCommand(transferId, userId, dto.Reason));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("shops/{shopId}/pending")]
    [ProducesResponseType(typeof(IEnumerable<StockTransferDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StockTransferDto>>> GetPendingTransfers(
        string shopId,
        [FromQuery] bool isSender = true)
    {
        var result = await _mediator.Send(new GetPendingTransfersQuery(shopId, isSender));
        return Ok(result);
    }

    [HttpGet("shops/{shopId}/history")]
    [ProducesResponseType(typeof(IEnumerable<StockTransferDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StockTransferDto>>> GetTransferHistory(
        string shopId,
        [FromQuery] bool isSender = true,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null,
        [FromQuery] int? limit = 100)
    {
        var query = new GetTransferHistoryQuery(shopId, isSender, startDate, endDate, status, limit);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

public class CancelTransferDto
{
    public string Reason { get; set; } = string.Empty;
}
