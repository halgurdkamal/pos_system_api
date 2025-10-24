using pos_system_api.Core.Application.Inventory.DTOs;
using MediatR;

namespace pos_system_api.Core.Application.Inventory.Commands.MoveToStorage;

/// <summary>
/// Command to move stock from shop floor back to storage
/// </summary>
public record MoveToStorageCommand(
    string ShopId,
    string DrugId,
    int Quantity,
    string? BatchNumber = null  // Optional: move specific batch
) : IRequest<InventoryDto>;
