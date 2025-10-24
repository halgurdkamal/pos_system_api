using pos_system_api.Core.Application.Inventory.DTOs;
using MediatR;

namespace pos_system_api.Core.Application.Inventory.Commands.MoveToShopFloor;

/// <summary>
/// Command to move stock from storage to shop floor
/// </summary>
public record MoveToShopFloorCommand(
    string ShopId,
    string DrugId,
    int Quantity,
    string? BatchNumber = null  // Optional: move specific batch
) : IRequest<InventoryDto>;
