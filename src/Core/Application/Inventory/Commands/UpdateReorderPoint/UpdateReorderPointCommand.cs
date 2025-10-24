using MediatR;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdateReorderPoint;

/// <summary>
/// Command to update reorder point for shop inventory
/// </summary>
public record UpdateReorderPointCommand(
    string ShopId,
    string DrugId,
    int ReorderPoint
) : IRequest<InventoryDto>;
