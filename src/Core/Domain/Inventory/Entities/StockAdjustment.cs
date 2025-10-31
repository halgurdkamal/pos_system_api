using pos_system_api.Core.Domain.Common;

namespace pos_system_api.Core.Domain.Inventory.Entities;

/// <summary>
/// Records all inventory adjustments for audit trail and regulatory compliance
/// </summary>
public class StockAdjustment : BaseEntity
{
    public string ShopId { get; private set; } = string.Empty;
    public string DrugId { get; private set; } = string.Empty;
    public string? BatchNumber { get; private set; }

    public AdjustmentType AdjustmentType { get; private set; }
    public int QuantityChanged { get; private set; }  // Positive = increase, Negative = decrease
    public int QuantityBefore { get; private set; }
    public int QuantityAfter { get; private set; }

    public string Reason { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    public string AdjustedBy { get; private set; } = string.Empty;  // User ID
    public DateTime AdjustedAt { get; private set; }

    public string? ReferenceId { get; private set; }  // Reference to order, transfer, etc.
    public string? ReferenceType { get; private set; }  // "Order", "Transfer", "StockCount", etc.

    private StockAdjustment() { }  // EF Core

    public StockAdjustment(
        string shopId,
        string drugId,
        string? batchNumber,
        AdjustmentType adjustmentType,
        int quantityChanged,
        int quantityBefore,
        string reason,
        string adjustedBy,
        string? notes = null,
        string? referenceId = null,
        string? referenceType = null)
    {
        ShopId = shopId ?? throw new ArgumentNullException(nameof(shopId));
        DrugId = drugId ?? throw new ArgumentNullException(nameof(drugId));
        BatchNumber = batchNumber;
        AdjustmentType = adjustmentType;
        QuantityChanged = quantityChanged;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityBefore + quantityChanged;
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        AdjustedBy = adjustedBy ?? throw new ArgumentNullException(nameof(adjustedBy));
        AdjustedAt = DateTime.UtcNow;
        Notes = notes;
        ReferenceId = referenceId;
        ReferenceType = referenceType;
    }
}

/// <summary>
/// Types of inventory adjustments
/// </summary>
public enum AdjustmentType
{
    /// <summary>Sale transaction (decrease)</summary>
    Sale = 0,

    /// <summary>Customer return (increase)</summary>
    Return = 1,

    /// <summary>Damaged goods (decrease)</summary>
    Damage = 2,

    /// <summary>Expired items (decrease)</summary>
    Expired = 3,

    /// <summary>Theft/loss (decrease)</summary>
    Theft = 4,

    /// <summary>Manual correction from stock count (increase or decrease)</summary>
    Correction = 5,

    /// <summary>Transfer to another shop (decrease)</summary>
    TransferOut = 6,

    /// <summary>Transfer from another shop (increase)</summary>
    TransferIn = 7,

    /// <summary>Initial stock receipt (increase)</summary>
    Receipt = 8,

    /// <summary>Location movement (shop floor ↔ storage)</summary>
    LocationMove = 9,

    /// <summary>Recall/quarantine (decrease from active)</summary>
    Recall = 10
}
