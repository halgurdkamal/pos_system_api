using MediatR;

namespace pos_system_api.Core.Application.Inventory.Queries.GetTotalStockValue;

/// <summary>
/// Query to get total stock value for a shop
/// </summary>
public record GetTotalStockValueQuery(string ShopId) : IRequest<decimal>;
