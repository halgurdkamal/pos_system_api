using pos_system_api.Core.Domain.Common;

namespace pos_system_api.Core.Domain.Inventory.Entities;

/// <summary>
/// Represents a stock transfer between two shops
/// </summary>
public class StockTransfer : BaseEntity
{
    public string FromShopId { get; private set; } = string.Empty;
    public string ToShopId { get; private set; } = string.Empty;
    public string DrugId { get; private set; } = string.Empty;
    public string? BatchNumber { get; private set; }

    public int Quantity { get; private set; }
    public TransferStatus Status { get; private set; }

    public string InitiatedBy { get; private set; } = string.Empty;  // User ID
    public DateTime InitiatedAt { get; private set; }

    public string? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }

    public string? ReceivedBy { get; private set; }
    public DateTime? ReceivedAt { get; private set; }

    public string? CancelledBy { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }

    public string? Notes { get; private set; }

    private StockTransfer() { }  // EF Core

    public StockTransfer(
        string fromShopId,
        string toShopId,
        string drugId,
        string? batchNumber,
        int quantity,
        string initiatedBy,
        string? notes = null)
    {
        FromShopId = fromShopId ?? throw new ArgumentNullException(nameof(fromShopId));
        ToShopId = toShopId ?? throw new ArgumentNullException(nameof(toShopId));
        DrugId = drugId ?? throw new ArgumentNullException(nameof(drugId));
        BatchNumber = batchNumber;
        Quantity = quantity > 0 ? quantity : throw new ArgumentException("Quantity must be positive", nameof(quantity));
        Status = TransferStatus.Pending;
        InitiatedBy = initiatedBy ?? throw new ArgumentNullException(nameof(initiatedBy));
        InitiatedAt = DateTime.UtcNow;
        Notes = notes;

        if (fromShopId == toShopId)
            throw new ArgumentException("Cannot transfer to the same shop");
    }

    public void Approve(string approvedBy)
    {
        if (Status != TransferStatus.Pending)
            throw new InvalidOperationException($"Cannot approve transfer with status {Status}");

        Status = TransferStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTime.UtcNow;
    }

    public void MarkInTransit()
    {
        if (Status != TransferStatus.Approved)
            throw new InvalidOperationException($"Cannot mark as in transit. Current status: {Status}");

        Status = TransferStatus.InTransit;
    }

    public void Complete(string receivedBy)
    {
        if (Status != TransferStatus.InTransit && Status != TransferStatus.Approved)
            throw new InvalidOperationException($"Cannot complete transfer with status {Status}");

        Status = TransferStatus.Completed;
        ReceivedBy = receivedBy;
        ReceivedAt = DateTime.UtcNow;
    }

    public void Cancel(string cancelledBy, string reason)
    {
        if (Status == TransferStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed transfer");

        Status = TransferStatus.Cancelled;
        CancelledBy = cancelledBy;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
    }
}

/// <summary>
/// Status of stock transfer
/// </summary>
public enum TransferStatus
{
    /// <summary>Transfer requested, awaiting approval</summary>
    Pending = 0,

    /// <summary>Transfer approved, ready to ship</summary>
    Approved = 1,

    /// <summary>Transfer in transit</summary>
    InTransit = 2,

    /// <summary>Transfer completed and received</summary>
    Completed = 3,

    /// <summary>Transfer cancelled</summary>
    Cancelled = 4
}
