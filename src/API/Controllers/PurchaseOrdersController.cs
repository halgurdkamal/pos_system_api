using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.PurchaseOrders.Commands.CreatePurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.Commands.SubmitPurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.Commands.ReceiveStock;
using pos_system_api.Core.Application.PurchaseOrders.Queries.GetPurchaseOrder;
using pos_system_api.Core.Application.PurchaseOrders.Queries.GetPurchaseOrderDashboard;
using pos_system_api.Core.Application.PurchaseOrders.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.PurchaseOrders.Entities;
using System.Security.Claims;

namespace pos_system_api.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseOrdersController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPurchaseOrderRepository _repository;
    private readonly ILogger<PurchaseOrdersController> _logger;

    public PurchaseOrdersController(
        IMediator mediator,
        IPurchaseOrderRepository repository,
        ILogger<PurchaseOrdersController> logger)
    {
        _mediator = mediator;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Create a new purchase order (Draft status)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderCommand command)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var commandWithUser = command with { CreatedBy = userId };

        var result = await _mediator.Send(commandWithUser);
        return CreatedAtAction(nameof(GetPurchaseOrder), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get purchase order by ID with full details
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrder(string id)
    {
        var result = await _mediator.Send(new GetPurchaseOrderQuery(id));
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Get paginated list of purchase orders with filtering
    /// </summary>
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
        PurchaseOrderStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<PurchaseOrderStatus>(status, ignoreCase: true, out var s))
            statusEnum = s;

        OrderPriority? priorityEnum = null;
        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<OrderPriority>(priority, ignoreCase: true, out var p))
            priorityEnum = p;

        var (orders, totalCount) = await _repository.GetPagedAsync(
            shopId, supplierId, statusEnum, fromDate, toDate,
            priorityEnum, isPaid, searchTerm, page, pageSize);

        var dtos = orders.Select(MapToSummaryDto).ToList();

        return Ok(new PagedPurchaseOrdersDto
        {
            Orders = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    /// <summary>
    /// Submit purchase order to supplier (Draft → Submitted)
    /// </summary>
    [HttpPost("{id}/submit")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> SubmitPurchaseOrder(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var result = await _mediator.Send(new SubmitPurchaseOrderCommand
        {
            OrderId = id,
            SubmittedBy = userId
        });
        return Ok(result);
    }

    /// <summary>
    /// Confirm purchase order (Submitted → Confirmed)
    /// </summary>
    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> ConfirmPurchaseOrder(string id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var purchaseOrder = await _repository.GetByIdAsync(id);

        if (purchaseOrder == null)
            return NotFound();

        purchaseOrder.Confirm(userId);
        await _repository.UpdateAsync(purchaseOrder);

        var result = await _mediator.Send(new GetPurchaseOrderQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Receive stock and update inventory
    /// </summary>
    [HttpPost("{id}/receive")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> ReceiveStock(string id, [FromBody] ReceiveStockRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";

        var command = new ReceiveStockCommand
        {
            OrderId = id,
            ReceivedBy = userId,
            Items = request.Items
        };

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Cancel purchase order with reason
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> CancelPurchaseOrder(string id, [FromBody] CancelOrderRequestDto request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "system";
        var purchaseOrder = await _repository.GetByIdAsync(id);

        if (purchaseOrder == null)
            return NotFound();

        purchaseOrder.Cancel(userId, request.Reason);
        await _repository.UpdateAsync(purchaseOrder);

        var result = await _mediator.Send(new GetPurchaseOrderQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Mark purchase order as paid
    /// </summary>
    [HttpPost("{id}/mark-paid")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<PurchaseOrderDto>> MarkAsPaid(string id, [FromBody] MarkAsPaidRequestDto? request = null)
    {
        var purchaseOrder = await _repository.GetByIdAsync(id);

        if (purchaseOrder == null)
            return NotFound();

        purchaseOrder.MarkAsPaid(request?.PaidAt);
        await _repository.UpdateAsync(purchaseOrder);

        var result = await _mediator.Send(new GetPurchaseOrderQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Get purchase order dashboard with analytics
    /// </summary>
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

    /// <summary>
    /// Get pending purchase orders (for quick access)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<PurchaseOrderSummaryDto>>> GetPendingOrders([FromQuery] string shopId)
    {
        var orders = await _repository.GetPendingOrdersAsync(shopId);
        return Ok(orders.Select(MapToSummaryDto).ToList());
    }

    /// <summary>
    /// Get overdue payment orders
    /// </summary>
    [HttpGet("overdue-payments")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<PurchaseOrderSummaryDto>>> GetOverduePayments([FromQuery] string shopId)
    {
        var orders = await _repository.GetOverduePaymentsAsync(shopId);
        return Ok(orders.Select(MapToSummaryDto).ToList());
    }

    /// <summary>
    /// Get supplier performance analytics
    /// </summary>
    [HttpGet("supplier-performance")]
    [Authorize(Policy = "ShopAccess")]
    public async Task<ActionResult<List<SupplierPerformanceDto>>> GetSupplierPerformance(
        [FromQuery] string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var spendingTask = _repository.GetSupplierSpendingAsync(shopId, fromDate, toDate);
        var orderCountTask = _repository.GetSupplierOrderCountAsync(shopId, fromDate, toDate);
        var deliveryTimeTask = _repository.GetSupplierAverageDeliveryTimeAsync(shopId);

        await Task.WhenAll(spendingTask, orderCountTask, deliveryTimeTask);

        var spending = await spendingTask;
        var orderCount = await orderCountTask;
        var deliveryTime = await deliveryTimeTask;

        var supplierIds = spending.Keys.Union(orderCount.Keys).ToList();
        var performances = supplierIds.Select(supplierId => new SupplierPerformanceDto
        {
            SupplierId = supplierId,
            SupplierName = supplierId, // Would normally fetch from supplier repository
            TotalOrders = orderCount.GetValueOrDefault(supplierId, 0),
            TotalSpending = spending.GetValueOrDefault(supplierId, 0),
            AverageDeliveryDays = deliveryTime.GetValueOrDefault(supplierId, 0),
            CompletedOrders = 0, // Would calculate from detailed data
            CancelledOrders = 0, // Would calculate from detailed data
            OnTimeDeliveryRate = 0 // Would calculate from detailed data
        }).OrderByDescending(p => p.TotalSpending).ToList();

        return Ok(performances);
    }

    private static PurchaseOrderSummaryDto MapToSummaryDto(PurchaseOrder po)
    {
        return new PurchaseOrderSummaryDto
        {
            Id = po.Id,
            OrderNumber = po.OrderNumber,
            ShopId = po.ShopId,
            SupplierId = po.SupplierId,
            Status = po.Status.ToString(),
            Priority = po.Priority.ToString(),
            TotalAmount = po.TotalAmount,
            OrderDate = po.OrderDate,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            IsPaid = po.IsPaid,
            ItemCount = po.Items.Count,
            CompletionPercentage = po.GetCompletionPercentage()
        };
    }
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

public record PagedPurchaseOrdersDto
{
    public List<PurchaseOrderSummaryDto> Orders { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}
