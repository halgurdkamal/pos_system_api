using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Sales.Commands.CancelSalesOrder;
using pos_system_api.Core.Application.Sales.Commands.CompleteSalesOrder;
using pos_system_api.Core.Application.Sales.Commands.ConfirmSalesOrder;
using pos_system_api.Core.Application.Sales.Commands.CreateSalesOrder;
using pos_system_api.Core.Application.Sales.Commands.ProcessPayment;
using pos_system_api.Core.Application.Sales.Commands.RefundSalesOrder;
using pos_system_api.Core.Application.Sales.DTOs;
using pos_system_api.Core.Application.Sales.Queries.GetCashierPerformance;
using pos_system_api.Core.Application.Sales.Queries.GetDraftOrders;
using pos_system_api.Core.Application.Sales.Queries.GetSalesByPaymentMethod;
using pos_system_api.Core.Application.Sales.Queries.GetSalesDashboard;
using pos_system_api.Core.Application.Sales.Queries.GetSalesOrder;
using pos_system_api.Core.Application.Sales.Queries.GetSalesOrders;
using pos_system_api.Core.Application.Sales.Queries.GetTodaysSalesOrders;
using pos_system_api.Core.Application.Sales.Queries.GetTopSellingDrugs;

namespace pos_system_api.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesOrdersController : BaseApiController
{
    private readonly IMediator _mediator;

    public SalesOrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Create a new sales order (cashier order).</summary>
    [HttpPost]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> CreateSalesOrder(
        [FromBody] CreateSalesOrderCommand command)
    {
        var commandWithCashier = command with { CashierId = CurrentUserId() };
        var result = await _mediator.Send(commandWithCashier);
        return CreatedAtAction(nameof(GetSalesOrder), new { id = result.Id }, result);
    }

    /// <summary>Get sales order by ID with full details.</summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> GetSalesOrder(string id)
    {
        var result = await _mediator.Send(new GetSalesOrderQuery(id));
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Get paginated list of sales orders with filtering.</summary>
    [HttpGet]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PagedSalesOrdersDto>> GetSalesOrders(
        [FromQuery] string? shopId = null,
        [FromQuery] string? cashierId = null,
        [FromQuery] string? customerId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? paymentMethod = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetSalesOrdersQuery(
            shopId, cashierId, customerId, status, fromDate, toDate, paymentMethod, searchTerm, page, pageSize));
        return Ok(result);
    }

    /// <summary>Confirm sales order (Draft → Confirmed).</summary>
    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> ConfirmSalesOrder(string id)
    {
        var result = await _mediator.Send(new ConfirmSalesOrderCommand(id));
        return Ok(result);
    }

    /// <summary>Process payment for sales order.</summary>
    [HttpPost("{id}/payment")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> ProcessPayment(
        string id, [FromBody] ProcessPaymentRequestDto request)
    {
        var result = await _mediator.Send(new ProcessPaymentCommand
        {
            OrderId = id,
            PaymentMethod = request.PaymentMethod,
            AmountPaid = request.AmountPaid,
            PaymentReference = request.PaymentReference,
        });
        return Ok(result);
    }

    /// <summary>Complete sales order (Paid → Completed).</summary>
    [HttpPost("{id}/complete")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> CompleteSalesOrder(string id)
    {
        var result = await _mediator.Send(new CompleteSalesOrderCommand(id));
        return Ok(result);
    }

    /// <summary>Cancel sales order with reason.</summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> CancelSalesOrder(
        string id, [FromBody] CancelSalesOrderRequestDto request)
    {
        var result = await _mediator.Send(
            new CancelSalesOrderCommand(id, CurrentUserId(), request.Reason));
        return Ok(result);
    }

    /// <summary>Refund sales order with reason.</summary>
    [HttpPost("{id}/refund")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> RefundSalesOrder(
        string id, [FromBody] RefundSalesOrderRequestDto request)
    {
        var result = await _mediator.Send(
            new RefundSalesOrderCommand(id, CurrentUserId(), request.Reason));
        return Ok(result);
    }

    /// <summary>Get sales dashboard with analytics.</summary>
    [HttpGet("dashboard")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDashboardDto>> GetDashboard(
        [FromQuery] string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _mediator.Send(new GetSalesDashboardQuery(shopId, fromDate, toDate));
        return Ok(result);
    }

    /// <summary>Get today's sales orders.</summary>
    [HttpGet("today")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<SalesOrderSummaryDto>>> GetTodaysOrders(
        [FromQuery] string shopId)
    {
        var result = await _mediator.Send(new GetTodaysSalesOrdersQuery(shopId));
        return Ok(result);
    }

    /// <summary>Get cashier performance analytics.</summary>
    [HttpGet("cashier-performance")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<CashierPerformanceDto>>> GetCashierPerformance(
        [FromQuery] string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _mediator.Send(new GetCashierPerformanceQuery(shopId, fromDate, toDate));
        return Ok(result);
    }

    /// <summary>Get sales by payment method.</summary>
    [HttpGet("sales-by-payment-method")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetSalesByPaymentMethod(
        [FromQuery] string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _mediator.Send(new GetSalesByPaymentMethodQuery(shopId, fromDate, toDate));
        return Ok(result);
    }

    /// <summary>Get top selling drugs.</summary>
    [HttpGet("top-selling-drugs")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<Dictionary<string, int>>> GetTopSellingDrugs(
        [FromQuery] string shopId,
        [FromQuery] int topCount = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _mediator.Send(
            new GetTopSellingDrugsQuery(shopId, topCount, fromDate, toDate));
        return Ok(result);
    }

    /// <summary>
    /// Get all draft (pending/paused) orders for multi-order cashier workflow.
    /// Cashiers can see all their paused orders to resume later.
    /// </summary>
    [HttpGet("drafts")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(List<SalesOrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SalesOrderSummaryDto>>> GetDraftOrders(
        [FromQuery] string? shopId = null,
        [FromQuery] string? cashierId = null)
    {
        cashierId ??= User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var result = await _mediator.Send(new GetDraftOrdersQuery(shopId, cashierId));
        return Ok(result);
    }

    /// <summary>
    /// Save current order as draft (pause it to start another order).
    /// Draft orders are already persisted; this just confirms the order exists
    /// and returns it for the explicit "pause" UI action.
    /// </summary>
    [HttpPost("{id}/save-draft")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SalesOrderDto>> SaveDraft(string id)
    {
        var result = await _mediator.Send(new GetSalesOrderQuery(id));
        return result == null ? NotFound(new { error = "Order not found" }) : Ok(result);
    }

    /// <summary>Resume/Continue working on a draft order.</summary>
    [HttpGet("{id}/resume")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SalesOrderDto>> ResumeOrder(string id)
    {
        var dto = await _mediator.Send(new GetSalesOrderQuery(id));
        if (dto == null)
        {
            return NotFound(new { error = "Order not found" });
        }
        if (!string.Equals(dto.Status, "Draft", StringComparison.Ordinal))
        {
            return BadRequest(new
            {
                error = $"Cannot resume order in status {dto.Status}. Only draft orders can be resumed.",
            });
        }
        return Ok(dto);
    }

    private string CurrentUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
}

// Request DTOs
public record ProcessPaymentRequestDto
{
    public string PaymentMethod { get; init; } = string.Empty;
    public decimal AmountPaid { get; init; }
    public string? PaymentReference { get; init; }
}

public record CancelSalesOrderRequestDto
{
    public string Reason { get; init; } = string.Empty;
}

public record RefundSalesOrderRequestDto
{
    public string Reason { get; init; } = string.Empty;
}
