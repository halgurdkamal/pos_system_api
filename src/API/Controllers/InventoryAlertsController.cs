using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.API.Controllers;

// DTOs
public class AlertDto
{
    public string Id { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int? CurrentQuantity { get; set; }
    public int? ThresholdQuantity { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime GeneratedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }
}

public class ResolveAlertDto
{
    public string? ResolutionNotes { get; set; }
}

public class AlertSummaryDto
{
    public int TotalActive { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public Dictionary<string, int> AlertTypeBreakdown { get; set; } = new();
}

// Commands & Queries
public record GenerateAlertsCommand(string ShopId) : IRequest<int>;
public record AcknowledgeAlertCommand(string AlertId) : IRequest<AlertDto>;
public record ResolveAlertCommand(string AlertId, string? ResolutionNotes) : IRequest<AlertDto>;
public record GetActiveAlertsQuery(string ShopId, string? Severity = null, string? AlertType = null) : IRequest<IEnumerable<AlertDto>>;
public record GetAlertSummaryQuery(string ShopId) : IRequest<AlertSummaryDto>;

// Handlers
public class GenerateAlertsHandler : IRequestHandler<GenerateAlertsCommand, int>
{
    private readonly IInventoryAlertService _alertService;
    private readonly IInventoryAlertRepository _repository;

    public GenerateAlertsHandler(IInventoryAlertService alertService, IInventoryAlertRepository repository)
    {
        _alertService = alertService;
        _repository = repository;
    }

    public async Task<int> Handle(GenerateAlertsCommand request, CancellationToken cancellationToken)
    {
        var beforeCount = await _repository.GetActiveAlertCountAsync(request.ShopId, cancellationToken: cancellationToken);
        await _alertService.GenerateAlertsForShopAsync(request.ShopId, cancellationToken);
        var afterCount = await _repository.GetActiveAlertCountAsync(request.ShopId, cancellationToken: cancellationToken);
        return afterCount - beforeCount;
    }
}

public class AcknowledgeAlertHandler : IRequestHandler<AcknowledgeAlertCommand, AlertDto>
{
    private readonly IInventoryAlertRepository _repository;

    public AcknowledgeAlertHandler(IInventoryAlertRepository repository) => _repository = repository;

    public async Task<AlertDto> Handle(AcknowledgeAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _repository.GetByIdAsync(request.AlertId, cancellationToken);
        if (alert == null) throw new KeyNotFoundException($"Alert {request.AlertId} not found");

        alert.Acknowledge("User");
        await _repository.UpdateAsync(alert, cancellationToken);

        return MapToDto(alert);
    }

    private AlertDto MapToDto(InventoryAlert a) => new()
    {
        Id = a.Id, ShopId = a.ShopId, DrugId = a.DrugId, BatchNumber = a.BatchNumber,
        AlertType = a.AlertType.ToString(), Severity = a.Severity.ToString(), Status = a.Status.ToString(),
        Message = a.Message, CurrentQuantity = a.CurrentQuantity, ThresholdQuantity = a.ThresholdQuantity,
        ExpiryDate = a.ExpiryDate, GeneratedAt = a.GeneratedAt, AcknowledgedAt = a.AcknowledgedAt,
        AcknowledgedBy = a.AcknowledgedBy, ResolvedAt = a.ResolvedAt, ResolvedBy = a.ResolvedBy,
        ResolutionNotes = a.ResolutionNotes
    };
}

public class ResolveAlertHandler : IRequestHandler<ResolveAlertCommand, AlertDto>
{
    private readonly IInventoryAlertRepository _repository;

    public ResolveAlertHandler(IInventoryAlertRepository repository) => _repository = repository;

    public async Task<AlertDto> Handle(ResolveAlertCommand request, CancellationToken cancellationToken)
    {
        var alert = await _repository.GetByIdAsync(request.AlertId, cancellationToken);
        if (alert == null) throw new KeyNotFoundException($"Alert {request.AlertId} not found");

        alert.Resolve("User", request.ResolutionNotes);
        await _repository.UpdateAsync(alert, cancellationToken);

        return MapToDto(alert);
    }

    private AlertDto MapToDto(InventoryAlert a) => new()
    {
        Id = a.Id, ShopId = a.ShopId, DrugId = a.DrugId, BatchNumber = a.BatchNumber,
        AlertType = a.AlertType.ToString(), Severity = a.Severity.ToString(), Status = a.Status.ToString(),
        Message = a.Message, CurrentQuantity = a.CurrentQuantity, ThresholdQuantity = a.ThresholdQuantity,
        ExpiryDate = a.ExpiryDate, GeneratedAt = a.GeneratedAt, AcknowledgedAt = a.AcknowledgedAt,
        AcknowledgedBy = a.AcknowledgedBy, ResolvedAt = a.ResolvedAt, ResolvedBy = a.ResolvedBy,
        ResolutionNotes = a.ResolutionNotes
    };
}

public class GetActiveAlertsHandler : IRequestHandler<GetActiveAlertsQuery, IEnumerable<AlertDto>>
{
    private readonly IInventoryAlertRepository _repository;

    public GetActiveAlertsHandler(IInventoryAlertRepository repository) => _repository = repository;

    public async Task<IEnumerable<AlertDto>> Handle(GetActiveAlertsQuery request, CancellationToken cancellationToken)
    {
        AlertSeverity? severity = null;
        if (!string.IsNullOrEmpty(request.Severity) && Enum.TryParse<AlertSeverity>(request.Severity, true, out var parsedSeverity))
            severity = parsedSeverity;

        AlertType? alertType = null;
        if (!string.IsNullOrEmpty(request.AlertType) && Enum.TryParse<AlertType>(request.AlertType, true, out var parsedType))
            alertType = parsedType;

        var alerts = await _repository.GetActiveAlertsAsync(request.ShopId, severity, alertType, cancellationToken);
        return alerts.Select(MapToDto);
    }

    private AlertDto MapToDto(InventoryAlert a) => new()
    {
        Id = a.Id, ShopId = a.ShopId, DrugId = a.DrugId, BatchNumber = a.BatchNumber,
        AlertType = a.AlertType.ToString(), Severity = a.Severity.ToString(), Status = a.Status.ToString(),
        Message = a.Message, CurrentQuantity = a.CurrentQuantity, ThresholdQuantity = a.ThresholdQuantity,
        ExpiryDate = a.ExpiryDate, GeneratedAt = a.GeneratedAt, AcknowledgedAt = a.AcknowledgedAt,
        AcknowledgedBy = a.AcknowledgedBy, ResolvedAt = a.ResolvedAt, ResolvedBy = a.ResolvedBy,
        ResolutionNotes = a.ResolutionNotes
    };
}

public class GetAlertSummaryHandler : IRequestHandler<GetAlertSummaryQuery, AlertSummaryDto>
{
    private readonly IInventoryAlertRepository _repository;

    public GetAlertSummaryHandler(IInventoryAlertRepository repository) => _repository = repository;

    public async Task<AlertSummaryDto> Handle(GetAlertSummaryQuery request, CancellationToken cancellationToken)
    {
        var alerts = await _repository.GetActiveAlertsAsync(request.ShopId, cancellationToken: cancellationToken);
        var alertList = alerts.ToList();

        return new AlertSummaryDto
        {
            TotalActive = alertList.Count,
            CriticalCount = alertList.Count(a => a.Severity == AlertSeverity.Critical),
            WarningCount = alertList.Count(a => a.Severity == AlertSeverity.Warning),
            InfoCount = alertList.Count(a => a.Severity == AlertSeverity.Info),
            AlertTypeBreakdown = alertList.GroupBy(a => a.AlertType.ToString())
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }
}

// Controller
[ApiController]
[Route("api/inventory-alerts")]
[Produces("application/json")]
[Authorize(Policy = "ShopAccess")]
public class InventoryAlertsController : BaseApiController
{
    private readonly IMediator _mediator;

    public InventoryAlertsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("shops/{shopId}/generate")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GenerateAlerts(string shopId)
    {
        var newAlerts = await _mediator.Send(new GenerateAlertsCommand(shopId));
        return Ok(new { message = $"Generated {newAlerts} new alerts", newAlerts });
    }

    [HttpGet("shops/{shopId}")]
    [ProducesResponseType(typeof(IEnumerable<AlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AlertDto>>> GetActiveAlerts(
        string shopId,
        [FromQuery] string? severity = null,
        [FromQuery] string? alertType = null)
    {
        var result = await _mediator.Send(new GetActiveAlertsQuery(shopId, severity, alertType));
        return Ok(result);
    }

    [HttpGet("shops/{shopId}/summary")]
    [ProducesResponseType(typeof(AlertSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertSummaryDto>> GetAlertSummary(string shopId)
    {
        var result = await _mediator.Send(new GetAlertSummaryQuery(shopId));
        return Ok(result);
    }

    [HttpPost("{alertId}/acknowledge")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertDto>> AcknowledgeAlert(string alertId)
    {
        try
        {
            var result = await _mediator.Send(new AcknowledgeAlertCommand(alertId));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{alertId}/resolve")]
    [ProducesResponseType(typeof(AlertDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AlertDto>> ResolveAlert(string alertId, [FromBody] ResolveAlertDto dto)
    {
        try
        {
            var result = await _mediator.Send(new ResolveAlertCommand(alertId, dto.ResolutionNotes));
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}
