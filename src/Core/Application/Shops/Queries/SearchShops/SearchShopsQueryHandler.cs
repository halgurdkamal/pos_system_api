using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Shops.Commands;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Queries.SearchShops;

/// <summary>
/// Handler for SearchShopsQuery
/// </summary>
public class SearchShopsQueryHandler : IRequestHandler<SearchShopsQuery, IEnumerable<ShopDto>>
{
    private readonly IShopRepository _shopRepository;

    public SearchShopsQueryHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<IEnumerable<ShopDto>> Handle(SearchShopsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return Enumerable.Empty<ShopDto>();
        }

        var shops = await _shopRepository.SearchByNameAsync(request.SearchTerm, cancellationToken);

        return shops.Select(ShopMapper.MapToDto).ToList();
    }
}
