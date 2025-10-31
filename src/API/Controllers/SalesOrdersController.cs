using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Sales.Commands.CreateSalesOrder;
using pos_system_api.Core.Application.Sales.Commands.ProcessPayment;
using pos_system_api.Core.Application.Sales.Queries.GetSalesOrder;
using pos_system_api.Core.Application.Sales.Queries.GetSalesDashboard;
using pos_system_api.Core.Application.Sales.Queries.GetDraftOrders;
using pos_system_api.Core.Application.Sales.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Sales.Entities;
using System.Security.Claims;

namespace pos_system_api.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesOrdersController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ISalesOrderRepository _repository;
    private readonly ILogger<SalesOrdersController> _logger;

    public SalesOrdersController(
        IMediator mediator,
        ISalesOrderRepository repository,
        ILogger<SalesOrdersController> logger)
    {
        _mediator = mediator;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Create a new sales order (cashier order)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> CreateSalesOrder([FromBody] CreateSalesOrderCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var commandWithCashier = command with { CashierId = userId };

        var result = await _mediator.Send(commandWithCashier);
        return CreatedAtAction(nameof(GetSalesOrder), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get sales order by ID with full details
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> GetSalesOrder(string id)
    {
        var result = await _mediator.Send(new GetSalesOrderQuery(id));
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Get paginated list of sales orders with filtering
    /// </summary>
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
        SalesOrderStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<SalesOrderStatus>(status, ignoreCase: true, out var s))
            statusEnum = s;

        PaymentMethod? paymentMethodEnum = null;
        if (!string.IsNullOrEmpty(paymentMethod) && Enum.TryParse<PaymentMethod>(paymentMethod, ignoreCase: true, out var pm))
            paymentMethodEnum = pm;

        var (orders, totalCount) = await _repository.GetPagedAsync(
            shopId, cashierId, customerId, statusEnum, fromDate, toDate,
            paymentMethodEnum, searchTerm, page, pageSize);

        var dtos = orders.Select(MapToSummaryDto).ToList();

        return Ok(new PagedSalesOrdersDto
        {
            Orders = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Confirm sales order (Draft → Confirmed)
    /// </summary>
    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> ConfirmSalesOrder(string id)
    {
        var salesOrder = await _repository.GetByIdAsync(id);

        if (salesOrder == null)
            return NotFound();

        salesOrder.Confirm();
        await _repository.UpdateAsync(salesOrder);

        var result = await _mediator.Send(new GetSalesOrderQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Process payment for sales order
    /// </summary>
    [HttpPost("{id}/payment")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> ProcessPayment(string id, [FromBody] ProcessPaymentRequestDto request)
    {
        var command = new ProcessPaymentCommand
        {
            OrderId = id,
            PaymentMethod = request.PaymentMethod,
            AmountPaid = request.AmountPaid,
            PaymentReference = request.PaymentReference
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Complete sales order (Paid → Completed)
    /// </summary>
    [HttpPost("{id}/complete")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> CompleteSalesOrder(string id)
    {
        var salesOrder = await _repository.GetByIdAsync(id);

        if (salesOrder == null)
            return NotFound();

        salesOrder.Complete();
        await _repository.UpdateAsync(salesOrder);

        var result = await _mediator.Send(new GetSalesOrderQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Cancel sales order with reason
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> CancelSalesOrder(string id, [FromBody] CancelSalesOrderRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var salesOrder = await _repository.GetByIdAsync(id);

        if (salesOrder == null)
            return NotFound();

        salesOrder.Cancel(userId, request.Reason);
        await _repository.UpdateAsync(salesOrder);

        var result = await _mediator.Send(new GetSalesOrderQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Refund sales order with reason
    /// </summary>
    [HttpPost("{id}/refund")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<SalesOrderDto>> RefundSalesOrder(string id, [FromBody] RefundSalesOrderRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var salesOrder = await _repository.GetByIdAsync(id);

        if (salesOrder == null)
            return NotFound();

        salesOrder.Refund(userId, request.Reason);
        await _repository.UpdateAsync(salesOrder);

        var result = await _mediator.Send(new GetSalesOrderQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Get sales dashboard with analytics
    /// </summary>
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

    /// <summary>
    /// Get today's sales orders
    /// </summary>
    [HttpGet("today")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<SalesOrderSummaryDto>>> GetTodaysOrders([FromQuery] string shopId)
    {
        var orders = await _repository.GetTodaysOrdersAsync(shopId);
        return Ok(orders.Select(MapToSummaryDto).ToList());
    }

    /// <summary>
    /// Get cashier performance analytics
    /// </summary>
    [HttpGet("cashier-performance")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<CashierPerformanceDto>>> GetCashierPerformance(
        [FromQuery] string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var salesTask = _repository.GetCashierSalesAsync(shopId, fromDate, toDate);
        var orderCountTask = _repository.GetCashierOrderCountAsync(shopId, fromDate, toDate);

        // Today's stats
        var today = DateTime.UtcNow.Date;
        var todaySalesTask = _repository.GetCashierSalesAsync(shopId, today, null);
        var todayOrderCountTask = _repository.GetCashierOrderCountAsync(shopId, today, null);

        await Task.WhenAll(salesTask, orderCountTask, todaySalesTask, todayOrderCountTask);

        var sales = await salesTask;
        var orderCount = await orderCountTask;
        var todaySales = await todaySalesTask;
        var todayOrderCount = await todayOrderCountTask;

        var cashierIds = sales.Keys.Union(orderCount.Keys).ToList();
        var performances = cashierIds.Select(cashierId => new CashierPerformanceDto
        {
            CashierId = cashierId,
            CashierName = cashierId, // Would normally fetch from user repository
            TotalOrders = orderCount.GetValueOrDefault(cashierId, 0),
            TotalSales = sales.GetValueOrDefault(cashierId, 0),
            AverageOrderValue = orderCount.GetValueOrDefault(cashierId, 0) > 0
                ? sales.GetValueOrDefault(cashierId, 0) / orderCount.GetValueOrDefault(cashierId, 0)
                : 0,
            TodaysOrders = todayOrderCount.GetValueOrDefault(cashierId, 0),
            TodaysSales = todaySales.GetValueOrDefault(cashierId, 0)
        }).OrderByDescending(p => p.TotalSales).ToList();

        return Ok(performances);
    }

    /// <summary>
    /// Get sales by payment method
    /// </summary>
    [HttpGet("sales-by-payment-method")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<Dictionary<string, decimal>>> GetSalesByPaymentMethod(
        [FromQuery] string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _repository.GetSalesByPaymentMethodAsync(shopId, fromDate, toDate);
        return Ok(result);
    }

    /// <summary>
    /// Get top selling drugs
    /// </summary>
    [HttpGet("top-selling-drugs")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<Dictionary<string, int>>> GetTopSellingDrugs(
        [FromQuery] string shopId,
        [FromQuery] int topCount = 10,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var result = await _repository.GetTopSellingDrugsAsync(shopId, topCount, fromDate, toDate);
        return Ok(result);
    }

    /// <summary>
    /// Get all draft (pending/paused) orders for multi-order cashier workflow
    /// Cashiers can see all their paused orders to resume later
    /// </summary>
    /// <param name="shopId">Optional: Filter by shop</param>
    /// <param name="cashierId">Optional: Filter by cashier (defaults to current user)</param>
    /// <returns>List of draft orders</returns>
    [HttpGet("drafts")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(List<SalesOrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SalesOrderSummaryDto>>> GetDraftOrders(
        [FromQuery] string? shopId = null,
        [FromQuery] string? cashierId = null)
    {
        // If no cashier specified, use current user
        if (string.IsNullOrEmpty(cashierId))
        {
            cashierId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        var query = new GetDraftOrdersQuery(shopId, cashierId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Save current order as draft (pause it to start another order)
    /// Note: Creating an order already sets it as Draft, this is just for updating
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Updated order</returns>
    [HttpPost("{id}/save-draft")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SalesOrderDto>> SaveDraft(string id)
    {
        var salesOrder = await _repository.GetByIdAsync(id);

        if (salesOrder == null)
            return NotFound(new { error = "Order not found" });

        // Draft orders are already saved, just return it
        // This endpoint exists for explicit "pause" action in UI
        var result = await _mediator.Send(new GetSalesOrderQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Resume/Continue working on a draft order
    /// </summary>
    /// <param name="id">Draft order ID to resume</param>
    /// <returns>Full order details</returns>
    [HttpGet("{id}/resume")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(SalesOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SalesOrderDto>> ResumeOrder(string id)
    {
        var salesOrder = await _repository.GetByIdAsync(id);

        if (salesOrder == null)
            return NotFound(new { error = "Order not found" });

        // Check if order is in Draft status
        if (salesOrder.Status != SalesOrderStatus.Draft)
            return BadRequest(new { error = $"Cannot resume order in status {salesOrder.Status}. Only draft orders can be resumed." });

        var result = await _mediator.Send(new GetSalesOrderQuery(id));
        return Ok(result);
    }

    private SalesOrderSummaryDto MapToSummaryDto(SalesOrder so)
    {
        return new SalesOrderSummaryDto
        {
            Id = so.Id,
            OrderNumber = so.OrderNumber,
            ShopId = so.ShopId,
            CustomerName = so.CustomerName,
            Status = so.Status.ToString(),
            TotalAmount = so.TotalAmount,
            OrderDate = so.OrderDate,
            CashierId = so.CashierId,
            ItemCount = so.Items.Count,
            PaymentMethod = so.PaymentMethod?.ToString()
        };
    }
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

public record PagedSalesOrdersDto
{
    public List<SalesOrderSummaryDto> Orders { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
