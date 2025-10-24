using MediatR;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Queries.GetLowStock;

/// <summary>
/// Query to get low stock items for a shop (below reorder point)
/// </summary>
public record GetLowStockQuery(string ShopId) : IRequest<IEnumerable<InventoryDto>>;
