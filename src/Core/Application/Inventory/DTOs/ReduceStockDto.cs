namespace pos_system_api.Core.Application.Inventory.DTOs;

/// <summary>
/// DTO for reducing stock from shop inventory
/// </summary>
public class ReduceStockDto
{
    public int Quantity { get; set; }
}
