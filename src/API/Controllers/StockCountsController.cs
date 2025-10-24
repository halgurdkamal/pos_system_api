using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.API.Controllers;

// DTOs
public class StockCountDto
{
    public string Id { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int SystemQuantity { get; set; }
    public int? PhysicalQuantity { get; set; }
    public int? VarianceQuantity { get; set; }
    public string? VarianceReason { get; set; }
    public string CountedBy { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public DateTime? CountedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}

public class CreateStockCountDto
{
    public string DrugId { get; set; } = string.Empty;
    public DateTime? ScheduledAt { get; set; }
    public string? Notes { get; set; }
}

public class RecordCountDto
{
    public int PhysicalQuantity { get; set; }
    public string? VarianceReason { get; set; }
}

// Commands & Queries
public record CreateStockCountCommand(string ShopId, string DrugId, string CountedBy, DateTime? ScheduledAt, string? Notes) : IRequest<StockCountDto>;
public record RecordCountCommand(string CountId, int PhysicalQuantity, string? VarianceReason) : IRequest<StockCountDto>;
public record CompleteStockCountCommand(string CountId) : IRequest<StockCountDto>;
public record GetStockCountsQuery(string ShopId, string? Status = null) : IRequest<IEnumerable<StockCountDto>>;

// Handlers
public class CreateStockCountHandler : IRequestHandler<CreateStockCountCommand, StockCountDto>
{
    private readonly IStockCountRepository _repository;
    private readonly IInventoryRepository _inventoryRepository;

    public CreateStockCountHandler(IStockCountRepository repository, IInventoryRepository inventoryRepository)
    {
        _repository = repository;
        _inventoryRepository = inventoryRepository;
    }

    public async Task<StockCountDto> Handle(CreateStockCountCommand request, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(request.ShopId, request.DrugId, cancellationToken);
        if (inventory == null) throw new KeyNotFoundException($"Inventory not found");
        
        var stockCount = new StockCount(request.ShopId, request.DrugId, inventory.TotalStock, request.CountedBy, request.ScheduledAt, request.Notes);
        await _repository.AddAsync(stockCount, cancellationToken);
        
        return MapToDto(stockCount);
    }

    private StockCountDto MapToDto(StockCount sc) => new()
    {
        Id = sc.Id, ShopId = sc.ShopId, DrugId = sc.DrugId, Status = sc.Status.ToString(),
        SystemQuantity = sc.SystemQuantity, PhysicalQuantity = sc.PhysicalQuantity,
        VarianceQuantity = sc.VarianceQuantity, VarianceReason = sc.VarianceReason,
        CountedBy = sc.CountedBy, ScheduledAt = sc.ScheduledAt, CountedAt = sc.CountedAt,
        CompletedAt = sc.CompletedAt, Notes = sc.Notes
    };
}

public class RecordCountHandler : IRequestHandler<RecordCountCommand, StockCountDto>
{
    private readonly IStockCountRepository _repository;
    private readonly IStockAdjustmentRepository _adjustmentRepository;

    public RecordCountHandler(IStockCountRepository repository, IStockAdjustmentRepository adjustmentRepository)
    {
        _repository = repository;
        _adjustmentRepository = adjustmentRepository;
    }

    public async Task<StockCountDto> Handle(RecordCountCommand request, CancellationToken cancellationToken)
    {
        var stockCount = await _repository.GetByIdAsync(request.CountId, cancellationToken);
        if (stockCount == null) throw new KeyNotFoundException($"Stock count {request.CountId} not found");
        
        stockCount.RecordCount(request.PhysicalQuantity, request.VarianceReason);
        await _repository.UpdateAsync(stockCount, cancellationToken);
        
        // Create adjustment if there's variance
        if (stockCount.VarianceQuantity.HasValue && stockCount.VarianceQuantity.Value != 0)
        {
            var adjustment = new StockAdjustment(
                stockCount.ShopId, stockCount.DrugId, null, AdjustmentType.Correction,
                stockCount.VarianceQuantity.Value, stockCount.SystemQuantity,
                $"Stock count variance: {request.VarianceReason ?? "Physical count adjustment"}",
                stockCount.CountedBy, null, stockCount.Id, "StockCount");
            await _adjustmentRepository.AddAsync(adjustment, cancellationToken);
        }
        
        return MapToDto(stockCount);
    }

    private StockCountDto MapToDto(StockCount sc) => new()
    {
        Id = sc.Id, ShopId = sc.ShopId, DrugId = sc.DrugId, Status = sc.Status.ToString(),
        SystemQuantity = sc.SystemQuantity, PhysicalQuantity = sc.PhysicalQuantity,
        VarianceQuantity = sc.VarianceQuantity, VarianceReason = sc.VarianceReason,
        CountedBy = sc.CountedBy, ScheduledAt = sc.ScheduledAt, CountedAt = sc.CountedAt,
        CompletedAt = sc.CompletedAt, Notes = sc.Notes
    };
}

public class CompleteStockCountHandler : IRequestHandler<CompleteStockCountCommand, StockCountDto>
{
    private readonly IStockCountRepository _repository;

    public CompleteStockCountHandler(IStockCountRepository repository) => _repository = repository;

    public async Task<StockCountDto> Handle(CompleteStockCountCommand request, CancellationToken cancellationToken)
    {
        var stockCount = await _repository.GetByIdAsync(request.CountId, cancellationToken);
        if (stockCount == null) throw new KeyNotFoundException($"Stock count {request.CountId} not found");
        
        stockCount.Complete();
        await _repository.UpdateAsync(stockCount, cancellationToken);
        
        return MapToDto(stockCount);
    }

    private StockCountDto MapToDto(StockCount sc) => new()
    {
        Id = sc.Id, ShopId = sc.ShopId, DrugId = sc.DrugId, Status = sc.Status.ToString(),
        SystemQuantity = sc.SystemQuantity, PhysicalQuantity = sc.PhysicalQuantity,
        VarianceQuantity = sc.VarianceQuantity, VarianceReason = sc.VarianceReason,
        CountedBy = sc.CountedBy, ScheduledAt = sc.ScheduledAt, CountedAt = sc.CountedAt,
        CompletedAt = sc.CompletedAt, Notes = sc.Notes
    };
}

public class GetStockCountsHandler : IRequestHandler<GetStockCountsQuery, IEnumerable<StockCountDto>>
{
    private readonly IStockCountRepository _repository;

    public GetStockCountsHandler(IStockCountRepository repository) => _repository = repository;

    public async Task<IEnumerable<StockCountDto>> Handle(GetStockCountsQuery request, CancellationToken cancellationToken)
    {
        StockCountStatus? status = null;
        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<StockCountStatus>(request.Status, true, out var parsed))
            status = parsed;
        
        var counts = await _repository.GetByShopAsync(request.ShopId, status, cancellationToken);
        return counts.Select(MapToDto);
    }

    private StockCountDto MapToDto(StockCount sc) => new()
    {
        Id = sc.Id, ShopId = sc.ShopId, DrugId = sc.DrugId, Status = sc.Status.ToString(),
        SystemQuantity = sc.SystemQuantity, PhysicalQuantity = sc.PhysicalQuantity,
        VarianceQuantity = sc.VarianceQuantity, VarianceReason = sc.VarianceReason,
        CountedBy = sc.CountedBy, ScheduledAt = sc.ScheduledAt, CountedAt = sc.CountedAt,
        CompletedAt = sc.CompletedAt, Notes = sc.Notes
    };
}

// Controller
[ApiController]
[Route("api/stock-counts")]
[Produces("application/json")]
[Authorize(Policy = "ShopAccess")]
public class StockCountsController : BaseApiController
{
    private readonly IMediator _mediator;

    public StockCountsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("shops/{shopId}")]
    [ProducesResponseType(typeof(StockCountDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<StockCountDto>> CreateStockCount(string shopId, [FromBody] CreateStockCountDto dto)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "System";
            var command = new CreateStockCountCommand(shopId, dto.DrugId, userId, dto.ScheduledAt, dto.Notes);
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetStockCounts), new { shopId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }

    [HttpPost("{countId}/record")]
    [ProducesResponseType(typeof(StockCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockCountDto>> RecordCount(string countId, [FromBody] RecordCountDto dto)
    {
        try
        {
            var result = await _mediator.Send(new RecordCountCommand(countId, dto.PhysicalQuantity, dto.VarianceReason));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{countId}/complete")]
    [ProducesResponseType(typeof(StockCountDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockCountDto>> CompleteCount(string countId)
    {
        try
        {
            var result = await _mediator.Send(new CompleteStockCountCommand(countId));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("shops/{shopId}")]
    [ProducesResponseType(typeof(IEnumerable<StockCountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<StockCountDto>>> GetStockCounts(
        string shopId,
        [FromQuery] string? status = null)
    {
        var result = await _mediator.Send(new GetStockCountsQuery(shopId, status));
        return Ok(result);
    }
}
