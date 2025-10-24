using MediatR;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Queries.GetShopById;

/// <summary>
/// Query to get a shop by its ID
/// </summary>
public record GetShopByIdQuery(string ShopId) : IRequest<ShopDto?>;
