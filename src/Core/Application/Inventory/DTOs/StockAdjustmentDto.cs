namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// DTO for stock adjustment records
/// </summary>
public class StockAdjustmentDto
{
    public string Id { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }

    public string AdjustmentType { get; set; } = string.Empty;
    public int QuantityChanged { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }

    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public string AdjustedBy { get; set; } = string.Empty;
    public DateTime AdjustedAt { get; set; }

    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
}

/// <summary>
/// DTO for creating a stock adjustment
/// </summary>
public class CreateStockAdjustmentDto
{
    public string DrugId { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string AdjustmentType { get; set; } = string.Empty;  // "Sale", "Return", "Damage", etc.
    public int QuantityChanged { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
}
