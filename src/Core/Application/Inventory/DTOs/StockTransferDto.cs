namespace pos_system_api.Core.Application.Inventory.DTOs;

public class StockTransferDto
{
    public string Id { get; set; } = string.Empty;
    public string FromShopId { get; set; } = string.Empty;
    public string ToShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public DateTime InitiatedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ReceivedBy { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }
}

public class CreateTransferDto
{
    public string ToShopId { get; set; } = string.Empty;
    public string DrugId { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}
