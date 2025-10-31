using pos_system_api.Core.Domain.Common;

namespace pos_system_api.Core.Domain.Inventory.Entities;

public class StockCount : BaseEntity
{
    public string ShopId { get; private set; } = string.Empty;
    public string DrugId { get; private set; } = string.Empty;
    public StockCountStatus Status { get; private set; }
    public int SystemQuantity { get; private set; }
    public int? PhysicalQuantity { get; private set; }
    public int? VarianceQuantity { get; private set; }
    public string? VarianceReason { get; private set; }
    public string CountedBy { get; private set; } = string.Empty;
    public DateTime ScheduledAt { get; private set; }
    public DateTime? CountedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Notes { get; private set; }

    private StockCount() { }

    public StockCount(string shopId, string drugId, int systemQuantity, string countedBy, DateTime? scheduledAt = null, string? notes = null)
    {
        ShopId = shopId ?? throw new ArgumentNullException(nameof(shopId));
        DrugId = drugId ?? throw new ArgumentNullException(nameof(drugId));
        SystemQuantity = systemQuantity;
        CountedBy = countedBy ?? throw new ArgumentNullException(nameof(countedBy));
        ScheduledAt = scheduledAt ?? DateTime.UtcNow;
        Status = StockCountStatus.Scheduled;
        Notes = notes;
    }

    public void StartCount()
    {
        if (Status != StockCountStatus.Scheduled)
            throw new InvalidOperationException($"Cannot start count with status {Status}");
        Status = StockCountStatus.InProgress;
    }

    public void RecordCount(int physicalQuantity, string? varianceReason = null)
    {
        if (Status != StockCountStatus.InProgress && Status != StockCountStatus.Scheduled)
            throw new InvalidOperationException($"Cannot record count with status {Status}");

        PhysicalQuantity = physicalQuantity;
        VarianceQuantity = physicalQuantity - SystemQuantity;
        VarianceReason = varianceReason;
        CountedAt = DateTime.UtcNow;
        Status = StockCountStatus.InProgress;
    }

    public void Complete()
    {
        if (Status != StockCountStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete count with status {Status}");
        if (!PhysicalQuantity.HasValue)
            throw new InvalidOperationException("Physical quantity must be recorded before completing");

        Status = StockCountStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }
}

public enum StockCountStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2
}
