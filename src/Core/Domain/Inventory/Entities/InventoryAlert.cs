using pos_system_api.Core.Domain.Common;

namespace pos_system_api.Core.Domain.Inventory.Entities;

public enum AlertType
{
    LowStock,
    OutOfStock,
    Expired,
    ExpiringSoon30Days,
    ExpiringSoon60Days,
    ExpiringSoon90Days,
    StockDiscrepancy,
    OverStock
}

public enum AlertSeverity
{
    Info,
    Warning,
    Critical
}

public enum AlertStatus
{
    Active,
    Acknowledged,
    Resolved,
    Dismissed
}

public class InventoryAlert : BaseEntity
{
    public string ShopId { get; private set; }
    public string DrugId { get; private set; }
    public string? BatchNumber { get; private set; }
    public AlertType AlertType { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public AlertStatus Status { get; private set; }
    public string Message { get; private set; }
    public int? CurrentQuantity { get; private set; }
    public int? ThresholdQuantity { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public string? AcknowledgedBy { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedBy { get; private set; }
    public string? ResolutionNotes { get; private set; }

    private InventoryAlert() { } // EF Core

    public InventoryAlert(
        string shopId,
        string drugId,
        string? batchNumber,
        AlertType alertType,
        AlertSeverity severity,
        string message,
        int? currentQuantity = null,
        int? thresholdQuantity = null,
        DateTime? expiryDate = null)
    {
        Id = Guid.NewGuid().ToString();
        ShopId = shopId;
        DrugId = drugId;
        BatchNumber = batchNumber;
        AlertType = alertType;
        Severity = severity;
        Status = AlertStatus.Active;
        Message = message;
        CurrentQuantity = currentQuantity;
        ThresholdQuantity = thresholdQuantity;
        ExpiryDate = expiryDate;
        GeneratedAt = DateTime.UtcNow;
    }

    public void Acknowledge(string acknowledgedBy)
    {
        if (Status != AlertStatus.Active)
            throw new InvalidOperationException($"Cannot acknowledge alert in status {Status}");

        Status = AlertStatus.Acknowledged;
        AcknowledgedAt = DateTime.UtcNow;
        AcknowledgedBy = acknowledgedBy;
    }

    public void Resolve(string resolvedBy, string? resolutionNotes = null)
    {
        if (Status == AlertStatus.Resolved || Status == AlertStatus.Dismissed)
            throw new InvalidOperationException($"Alert is already {Status}");

        Status = AlertStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        ResolutionNotes = resolutionNotes;
    }

    public void Dismiss(string dismissedBy, string? reason = null)
    {
        if (Status == AlertStatus.Resolved || Status == AlertStatus.Dismissed)
            throw new InvalidOperationException($"Alert is already {Status}");

        Status = AlertStatus.Dismissed;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = dismissedBy;
        ResolutionNotes = reason;
    }
}
