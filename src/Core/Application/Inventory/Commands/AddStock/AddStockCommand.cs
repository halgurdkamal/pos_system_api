using MediatR;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Commands.AddStock;

/// <summary>
/// Command to add stock (creates a new batch) to shop inventory
/// </summary>
public record AddStockCommand(
    string ShopId,
    string DrugId,
    string BatchNumber,
    string SupplierId,
    int Quantity,
    DateTime ExpiryDate,
    decimal PurchasePrice,
    decimal SellingPrice,
    string StorageLocation,
    int? ReorderPoint = null
) : IRequest<InventoryDto>;
