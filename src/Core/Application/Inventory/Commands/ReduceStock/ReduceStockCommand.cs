using MediatR;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Commands.ReduceStock;

/// <summary>
/// Command to reduce stock from shop inventory (FIFO)
/// </summary>
public record ReduceStockCommand(
    string ShopId,
    string DrugId,
    int Quantity
) : IRequest<InventoryDto>;
