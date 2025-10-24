using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.API.Controllers;

// Report DTOs
public class StockValuationReportDto
{
    public string ShopId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int TotalItems { get; set; }
    public int TotalUnits { get; set; }
    public decimal TotalValue { get; set; }
    public List<StockValuationItemDto> Items { get; set; } = new();
}

public class StockValuationItemDto
{
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
}

public class StockMovementReportDto
{
    public string ShopId { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<StockMovementItemDto> Movements { get; set; } = new();
}

public class StockMovementItemDto
{
    public DateTime Date { get; set; }
    public string DrugId { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;
    public int QuantityChanged { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ABCAnalysisReportDto
{
    public string ShopId { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; }
    public List<ABCCategoryDto> Categories { get; set; } = new();
}

public class ABCCategoryDto
{
    public string Category { get; set; } = string.Empty; // A, B, or C
    public int ItemCount { get; set; }
    public decimal TotalValue { get; set; }
    public decimal PercentageOfTotalValue { get; set; }
    public List<ABCItemDto> Items { get; set; } = new();
}

public class ABCItemDto
{
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

public class ExpiryReportDto
{
    public string ShopId { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int ExpiredBatches { get; set; }
    public int ExpiringSoon30Days { get; set; }
    public int ExpiringSoon60Days { get; set; }
    public int ExpiringSoon90Days { get; set; }
    public List<ExpiryItemDto> Items { get; set; } = new();
}

public class ExpiryItemDto
{
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string Status { get; set; } = string.Empty; // Expired, Expiring30, Expiring60, Expiring90
}

public class TurnoverReportDto
{
    public string ShopId { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<TurnoverItemDto> Items { get; set; } = new();
}

public class TurnoverItemDto
{
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public int TotalSold { get; set; }
    public int AverageStock { get; set; }
    public decimal TurnoverRate { get; set; }
    public int DaysOfSupply { get; set; }
}

public class DeadStockReportDto
{
    public string ShopId { get; set; } = string.Empty;
    public int DaysThreshold { get; set; }
    public List<DeadStockItemDto> Items { get; set; } = new();
}

public class DeadStockItemDto
{
    public string DrugId { get; set; } = string.Empty;
    public string DrugName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int DaysSinceLastMovement { get; set; }
    public DateTime? LastMovementDate { get; set; }
}

// Queries
public record GetStockValuationQuery(string ShopId) : IRequest<StockValuationReportDto>;
public record GetStockMovementQuery(string ShopId, DateTime FromDate, DateTime ToDate, string? DrugId = null) : IRequest<StockMovementReportDto>;
public record GetABCAnalysisQuery(string ShopId) : IRequest<ABCAnalysisReportDto>;
public record GetExpiryReportQuery(string ShopId, int DaysAhead = 90) : IRequest<ExpiryReportDto>;
public record GetTurnoverReportQuery(string ShopId, DateTime FromDate, DateTime ToDate) : IRequest<TurnoverReportDto>;
public record GetDeadStockQuery(string ShopId, int DaysThreshold = 180) : IRequest<DeadStockReportDto>;

// Query Handlers
public class GetStockValuationHandler : IRequestHandler<GetStockValuationQuery, StockValuationReportDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;

    public GetStockValuationHandler(IInventoryRepository inventoryRepository, IDrugRepository drugRepository)
    {
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
    }

    public async Task<StockValuationReportDto> Handle(GetStockValuationQuery request, CancellationToken cancellationToken)
    {
        var inventories = await _inventoryRepository.GetAllByShopAsync(request.ShopId, cancellationToken);
        var items = new List<StockValuationItemDto>();

        foreach (var inventory in inventories)
        {
            var drug = await _drugRepository.GetByIdAsync(inventory.DrugId, cancellationToken);
            if (drug == null) continue;

            decimal avgCost = inventory.Batches.Any() 
                ? inventory.Batches.Average(b => b.PurchasePrice) 
                : 0;

            items.Add(new StockValuationItemDto
            {
                DrugId = inventory.DrugId,
                DrugName = drug.BrandName,
                Quantity = inventory.TotalStock,
                UnitCost = avgCost,
                TotalValue = inventory.TotalStock * avgCost
            });
        }

        return new StockValuationReportDto
        {
            ShopId = request.ShopId,
            GeneratedAt = DateTime.UtcNow,
            TotalItems = items.Count,
            TotalUnits = items.Sum(i => i.Quantity),
            TotalValue = items.Sum(i => i.TotalValue),
            Items = items.OrderByDescending(i => i.TotalValue).ToList()
        };
    }
}

public class GetStockMovementHandler : IRequestHandler<GetStockMovementQuery, StockMovementReportDto>
{
    private readonly IStockAdjustmentRepository _adjustmentRepository;

    public GetStockMovementHandler(IStockAdjustmentRepository adjustmentRepository) 
        => _adjustmentRepository = adjustmentRepository;

    public async Task<StockMovementReportDto> Handle(GetStockMovementQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<StockAdjustment> adjustments;
        
        if (!string.IsNullOrEmpty(request.DrugId))
        {
            adjustments = await _adjustmentRepository.GetByDrugAsync(
                request.ShopId, 
                request.DrugId,
                request.FromDate, 
                request.ToDate, 
                cancellationToken);
        }
        else
        {
            adjustments = await _adjustmentRepository.GetByShopAsync(
                request.ShopId, 
                request.FromDate, 
                request.ToDate,
                null,
                null,
                cancellationToken);
        }

        var movements = adjustments.Select(a => new StockMovementItemDto
        {
            Date = a.AdjustedAt,
            DrugId = a.DrugId,
            BatchNumber = a.BatchNumber,
            AdjustmentType = a.AdjustmentType.ToString(),
            QuantityChanged = a.QuantityChanged,
            QuantityBefore = a.QuantityBefore,
            QuantityAfter = a.QuantityAfter,
            Reason = a.Reason
        }).ToList();

        return new StockMovementReportDto
        {
            ShopId = request.ShopId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Movements = movements
        };
    }
}

public class GetABCAnalysisHandler : IRequestHandler<GetABCAnalysisQuery, ABCAnalysisReportDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;

    public GetABCAnalysisHandler(IInventoryRepository inventoryRepository, IDrugRepository drugRepository)
    {
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
    }

    public async Task<ABCAnalysisReportDto> Handle(GetABCAnalysisQuery request, CancellationToken cancellationToken)
    {
        var inventories = await _inventoryRepository.GetAllByShopAsync(request.ShopId, cancellationToken);
        var itemsWithValue = new List<(string drugId, string drugName, decimal value)>();

        foreach (var inventory in inventories)
        {
            var drug = await _drugRepository.GetByIdAsync(inventory.DrugId, cancellationToken);
            if (drug == null) continue;

            decimal avgCost = inventory.Batches.Any() 
                ? inventory.Batches.Average(b => b.PurchasePrice) 
                : 0;
            decimal value = inventory.TotalStock * avgCost;

            itemsWithValue.Add((inventory.DrugId, drug.BrandName, value));
        }

        var sortedItems = itemsWithValue.OrderByDescending(i => i.value).ToList();
        decimal totalValue = sortedItems.Sum(i => i.value);

        // ABC Analysis: A = 70%, B = 20%, C = 10%
        var categories = new List<ABCCategoryDto>();
        decimal cumulativeValue = 0;
        var categoryA = new List<ABCItemDto>();
        var categoryB = new List<ABCItemDto>();
        var categoryC = new List<ABCItemDto>();

        foreach (var item in sortedItems)
        {
            cumulativeValue += item.value;
            var percentage = totalValue > 0 ? (cumulativeValue / totalValue) * 100 : 0;

            var abcItem = new ABCItemDto
            {
                DrugId = item.drugId,
                DrugName = item.drugName,
                TotalValue = item.value,
                PercentageOfTotal = totalValue > 0 ? (item.value / totalValue) * 100 : 0
            };

            if (percentage <= 70)
                categoryA.Add(abcItem);
            else if (percentage <= 90)
                categoryB.Add(abcItem);
            else
                categoryC.Add(abcItem);
        }

        if (categoryA.Any())
            categories.Add(new ABCCategoryDto
            {
                Category = "A",
                ItemCount = categoryA.Count,
                TotalValue = categoryA.Sum(i => i.TotalValue),
                PercentageOfTotalValue = totalValue > 0 ? (categoryA.Sum(i => i.TotalValue) / totalValue) * 100 : 0,
                Items = categoryA
            });

        if (categoryB.Any())
            categories.Add(new ABCCategoryDto
            {
                Category = "B",
                ItemCount = categoryB.Count,
                TotalValue = categoryB.Sum(i => i.TotalValue),
                PercentageOfTotalValue = totalValue > 0 ? (categoryB.Sum(i => i.TotalValue) / totalValue) * 100 : 0,
                Items = categoryB
            });

        if (categoryC.Any())
            categories.Add(new ABCCategoryDto
            {
                Category = "C",
                ItemCount = categoryC.Count,
                TotalValue = categoryC.Sum(i => i.TotalValue),
                PercentageOfTotalValue = totalValue > 0 ? (categoryC.Sum(i => i.TotalValue) / totalValue) * 100 : 0,
                Items = categoryC
            });

        return new ABCAnalysisReportDto
        {
            ShopId = request.ShopId,
            AnalysisDate = DateTime.UtcNow,
            Categories = categories
        };
    }
}

public class GetExpiryReportHandler : IRequestHandler<GetExpiryReportQuery, ExpiryReportDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;

    public GetExpiryReportHandler(IInventoryRepository inventoryRepository, IDrugRepository drugRepository)
    {
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
    }

    public async Task<ExpiryReportDto> Handle(GetExpiryReportQuery request, CancellationToken cancellationToken)
    {
        var inventories = await _inventoryRepository.GetAllByShopAsync(request.ShopId, cancellationToken);
        var items = new List<ExpiryItemDto>();
        var now = DateTime.UtcNow;
        int expired = 0, expiring30 = 0, expiring60 = 0, expiring90 = 0;

        foreach (var inventory in inventories)
        {
            var drug = await _drugRepository.GetByIdAsync(inventory.DrugId, cancellationToken);
            if (drug == null) continue;

            foreach (var batch in inventory.Batches.Where(b => b.QuantityOnHand > 0))
            {
                var daysUntilExpiry = (int)(batch.ExpiryDate - now).TotalDays;
                string status;

                if (daysUntilExpiry < 0)
                {
                    status = "Expired";
                    expired++;
                }
                else if (daysUntilExpiry <= 30)
                {
                    status = "Expiring30";
                    expiring30++;
                }
                else if (daysUntilExpiry <= 60)
                {
                    status = "Expiring60";
                    expiring60++;
                }
                else if (daysUntilExpiry <= request.DaysAhead)
                {
                    status = "Expiring90";
                    expiring90++;
                }
                else
                    continue;

                items.Add(new ExpiryItemDto
                {
                    DrugId = inventory.DrugId,
                    DrugName = drug.BrandName,
                    BatchNumber = batch.BatchNumber,
                    Quantity = batch.QuantityOnHand,
                    ExpiryDate = batch.ExpiryDate,
                    DaysUntilExpiry = daysUntilExpiry,
                    Status = status
                });
            }
        }

        return new ExpiryReportDto
        {
            ShopId = request.ShopId,
            GeneratedAt = now,
            ExpiredBatches = expired,
            ExpiringSoon30Days = expiring30,
            ExpiringSoon60Days = expiring60,
            ExpiringSoon90Days = expiring90,
            Items = items.OrderBy(i => i.DaysUntilExpiry).ToList()
        };
    }
}

public class GetTurnoverReportHandler : IRequestHandler<GetTurnoverReportQuery, TurnoverReportDto>
{
    private readonly IStockAdjustmentRepository _adjustmentRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;

    public GetTurnoverReportHandler(
        IStockAdjustmentRepository adjustmentRepository,
        IInventoryRepository inventoryRepository,
        IDrugRepository drugRepository)
    {
        _adjustmentRepository = adjustmentRepository;
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
    }

    public async Task<TurnoverReportDto> Handle(GetTurnoverReportQuery request, CancellationToken cancellationToken)
    {
        var adjustments = await _adjustmentRepository.GetByShopAsync(
            request.ShopId, request.FromDate, request.ToDate, AdjustmentType.Sale, null, cancellationToken);
        var inventories = await _inventoryRepository.GetAllByShopAsync(request.ShopId, cancellationToken);
        var items = new List<TurnoverItemDto>();
        var daysDiff = Math.Max(1, (request.ToDate - request.FromDate).Days);

        var salesByDrug = adjustments.GroupBy(a => a.DrugId)
            .ToDictionary(g => g.Key, g => Math.Abs(g.Sum(a => a.QuantityChanged)));

        foreach (var inventory in inventories)
        {
            var drug = await _drugRepository.GetByIdAsync(inventory.DrugId, cancellationToken);
            if (drug == null) continue;

            var totalSold = salesByDrug.ContainsKey(inventory.DrugId) ? salesByDrug[inventory.DrugId] : 0;
            var avgStock = inventory.TotalStock; // Simplified - could calculate actual average
            var turnoverRate = avgStock > 0 ? (decimal)totalSold / avgStock : 0;
            var daysOfSupply = totalSold > 0 ? (int)(avgStock / ((decimal)totalSold / daysDiff)) : 0;

            items.Add(new TurnoverItemDto
            {
                DrugId = inventory.DrugId,
                DrugName = drug.BrandName,
                TotalSold = totalSold,
                AverageStock = avgStock,
                TurnoverRate = turnoverRate,
                DaysOfSupply = daysOfSupply
            });
        }

        return new TurnoverReportDto
        {
            ShopId = request.ShopId,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Items = items.OrderByDescending(i => i.TurnoverRate).ToList()
        };
    }
}

public class GetDeadStockHandler : IRequestHandler<GetDeadStockQuery, DeadStockReportDto>
{
    private readonly IStockAdjustmentRepository _adjustmentRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;

    public GetDeadStockHandler(
        IStockAdjustmentRepository adjustmentRepository,
        IInventoryRepository inventoryRepository,
        IDrugRepository drugRepository)
    {
        _adjustmentRepository = adjustmentRepository;
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
    }

    public async Task<DeadStockReportDto> Handle(GetDeadStockQuery request, CancellationToken cancellationToken)
    {
        var inventories = await _inventoryRepository.GetAllByShopAsync(request.ShopId, cancellationToken);
        var items = new List<DeadStockItemDto>();
        var thresholdDate = DateTime.UtcNow.AddDays(-request.DaysThreshold);

        foreach (var inventory in inventories)
        {
            if (inventory.TotalStock == 0) continue;

            var drug = await _drugRepository.GetByIdAsync(inventory.DrugId, cancellationToken);
            if (drug == null) continue;

            var adjustments = await _adjustmentRepository.GetByDrugAsync(
                request.ShopId, inventory.DrugId, thresholdDate, DateTime.UtcNow, cancellationToken);

            var lastMovement = adjustments.OrderByDescending(a => a.AdjustedAt).FirstOrDefault();
            
            if (lastMovement == null || lastMovement.AdjustedAt < thresholdDate)
            {
                var daysSinceMovement = lastMovement == null 
                    ? request.DaysThreshold + 1 
                    : (int)(DateTime.UtcNow - lastMovement.AdjustedAt).TotalDays;

                items.Add(new DeadStockItemDto
                {
                    DrugId = inventory.DrugId,
                    DrugName = drug.BrandName,
                    Quantity = inventory.TotalStock,
                    DaysSinceLastMovement = daysSinceMovement,
                    LastMovementDate = lastMovement?.AdjustedAt
                });
            }
        }

        return new DeadStockReportDto
        {
            ShopId = request.ShopId,
            DaysThreshold = request.DaysThreshold,
            Items = items.OrderByDescending(i => i.DaysSinceLastMovement).ToList()
        };
    }
}

// Controller
[ApiController]
[Route("api/inventory-reports")]
[Produces("application/json")]
[Authorize(Policy = "ShopAccess")]
public class InventoryReportsController : BaseApiController
{
    private readonly IMediator _mediator;

    public InventoryReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("shops/{shopId}/valuation")]
    [ProducesResponseType(typeof(StockValuationReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockValuationReportDto>> GetStockValuation(string shopId)
    {
        var result = await _mediator.Send(new GetStockValuationQuery(shopId));
        return Ok(result);
    }

    [HttpGet("shops/{shopId}/movement")]
    [ProducesResponseType(typeof(StockMovementReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<StockMovementReportDto>> GetStockMovement(
        string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? drugId = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _mediator.Send(new GetStockMovementQuery(shopId, from, to, drugId));
        return Ok(result);
    }

    [HttpGet("shops/{shopId}/abc-analysis")]
    [ProducesResponseType(typeof(ABCAnalysisReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ABCAnalysisReportDto>> GetABCAnalysis(string shopId)
    {
        var result = await _mediator.Send(new GetABCAnalysisQuery(shopId));
        return Ok(result);
    }

    [HttpGet("shops/{shopId}/expiry")]
    [ProducesResponseType(typeof(ExpiryReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExpiryReportDto>> GetExpiryReport(
        string shopId,
        [FromQuery] int daysAhead = 90)
    {
        var result = await _mediator.Send(new GetExpiryReportQuery(shopId, daysAhead));
        return Ok(result);
    }

    [HttpGet("shops/{shopId}/turnover")]
    [ProducesResponseType(typeof(TurnoverReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TurnoverReportDto>> GetTurnoverReport(
        string shopId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;
        var result = await _mediator.Send(new GetTurnoverReportQuery(shopId, from, to));
        return Ok(result);
    }

    [HttpGet("shops/{shopId}/dead-stock")]
    [ProducesResponseType(typeof(DeadStockReportDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DeadStockReportDto>> GetDeadStock(
        string shopId,
        [FromQuery] int daysThreshold = 180)
    {
        var result = await _mediator.Send(new GetDeadStockQuery(shopId, daysThreshold));
        return Ok(result);
    }
}
