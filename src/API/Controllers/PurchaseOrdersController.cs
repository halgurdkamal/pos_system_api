using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.PurchaseOrders.Commands.CancelPurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.Commands.ConfirmPurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.Commands.CreatePurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.Commands.MarkPurchaseOrderAsPaid;
using pos_system_api.Core.Application.PurchaseOrders.Commands.ReceiveStock;
using pos_system_api.Core.Application.PurchaseOrders.Commands.SubmitPurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;
using pos_system_api.Core.Application.PurchaseOrders.Queries.GetOverduePaymentPurchaseOrders;
using pos_system_api.Core.Application.PurchaseOrders.Queries.GetPendingPurchaseOrders;
using pos_system_api.Core.Application.PurchaseOrders.Queries.GetPurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.Queries.GetPurchaseOrderDashboard;
using pos_system_api.Core.Application.PurchaseOrders.Queries.GetPurchaseOrders;
using pos_system_api.Core.Application.PurchaseOrders.Queries.GetSupplierPerformance;

namespace pos_system_api.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseOrdersController : BaseApiController
{
    private readonly IMediator _mediator;

    public PurchaseOrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Create a new purchase order (Draft status).</summary>
    [HttpPost]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder(
        [FromBody] CreatePurchaseOrderCommand command)
    {
        var commandWithUser = command with { CreatedBy = CurrentUserId() };
        var result = await _mediator.Send(commandWithUser);
        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = result.Id }, result);
    }

    /// <summary>Get purchase order by ID with full details.</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrder(string id)
    {
        var result = await _mediator.Send(new GetPurchaseOrderQuery(id));
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Get paginated list of purchase orders with filtering.</summary>
    [HttpGet]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PagedPurchaseOrdersDto>> GetPurchaseOrders(
        [FromQuery] string? shopId = null,
        [FromQuery] string? supplierId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? priority = null,
        [FromQuery] bool? isPaid = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetPurchaseOrdersQuery(
            shopId, supplierId, status, fromDate, toDate, priority, isPaid, searchTerm, page, pageSize));
        return Ok(result);
    }

    /// <summary>Submit purchase order to supplier (Draft → Submitted).</summary>
    [HttpPost("{id}/submit")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> SubmitPurchaseOrder(string id)
    {
        var result = await _mediator.Send(
            new SubmitPurchaseOrderCommand { OrderId = id, SubmittedBy = CurrentUserId() });
        return Ok(result);
    }

    /// <summary>Confirm purchase order (Submitted → Confirmed).</summary>
    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> ConfirmPurchaseOrder(string id)
    {
        var result = await _mediator.Send(new ConfirmPurchaseOrderCommand(id, CurrentUserId()));
        return Ok(result);
    }

    /// <summary>Receive stock and update inventory.</summary>
    [HttpPost("{id}/receive")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> ReceiveStock(
        string id,
        [FromBody] ReceiveStockRequestDto request)
    {
        var result = await _mediator.Send(new ReceiveStockCommand
        {
            OrderId = id,
            ReceivedBy = CurrentUserId(),
            Items = request.Items,
        });
        return Ok(result);
    }

    /// <summary>Cancel purchase order with reason.</summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> CancelPurchaseOrder(
        string id,
        [FromBody] CancelOrderRequestDto request)
    {
        var result = await _mediator.Send(
            new CancelPurchaseOrderCommand(id, CurrentUserId(), request.Reason));
        return Ok(result);
    }

    /// <summary>Mark purchase order as paid.</summary>
    [HttpPost("{id}/mark-paid")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> MarkAsPaid(
        string id,
        [FromBody] MarkAsPaidRequestDto? request = null)
    {
        var result = await _mediator.Send(
            new MarkPurchaseOrderAsPaidCommand(id, request?.PaidAt));
        return Ok(result);
    }

    /// <summary>Get purchase order dashboard with analytics.</summary>
    [HttpGet("dashboard")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDashboardDto>> GetDashboard(
        [FromQuery] string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _mediator.Send(new GetPurchaseOrderDashboardQuery(shopId, fromDate, toDate));
        return Ok(result);
    }

    /// <summary>Get pending purchase orders (for quick access).</summary>
    [HttpGet("pending")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<PurchaseOrderSummaryDto>>> GetPendingOrders(
        [FromQuery] string shopId)
    {
        var result = await _mediator.Send(new GetPendingPurchaseOrdersQuery(shopId));
        return Ok(result);
    }

    /// <summary>Get overdue payment orders.</summary>
    [HttpGet("overdue-payments")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<PurchaseOrderSummaryDto>>> GetOverduePayments(
        [FromQuery] string shopId)
    {
        var result = await _mediator.Send(new GetOverduePaymentPurchaseOrdersQuery(shopId));
        return Ok(result);
    }

    /// <summary>Get supplier performance analytics.</summary>
    [HttpGet("supplier-performance")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<SupplierPerformanceDto>>> GetSupplierPerformance(
        [FromQuery] string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _mediator.Send(new GetSupplierPerformanceQuery(shopId, fromDate, toDate));
        return Ok(result);
    }

    private string CurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
}

// Request DTOs
public record ReceiveStockRequestDto
{
    public List<ReceiveStockItemDto> Items { get; init; } = new();
}

public record CancelOrderRequestDto
{
    public string Reason { get; init; } = string.Empty;
}

public record MarkAsPaidRequestDto
{
    public DateTime? PaidAt { get; init; }
}
