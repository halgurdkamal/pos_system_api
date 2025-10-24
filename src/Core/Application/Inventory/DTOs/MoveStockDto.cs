namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// DTO for moving stock between locations (shop floor <-> storage)
/// </summary>
public class MoveStockDto
{
    public int Quantity { get; set; }
    public string? BatchNumber { get; set; }  // Optional: move specific batch
}
