using MediatR;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Queries.SearchShops;

/// <summary>
/// Query to search shops by name (shop name or legal name)
/// </summary>
public record SearchShopsQuery(string SearchTerm) : IRequest<IEnumerable<ShopDto>>;
