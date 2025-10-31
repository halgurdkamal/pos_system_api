using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Shops.Commands;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Queries.GetShopById;

/// <summary>
/// Handler for GetShopByIdQuery
/// </summary>
public class GetShopByIdQueryHandler : IRequestHandler<GetShopByIdQuery, ShopDto?>
{
    private readonly IShopRepository _shopRepository;

    public GetShopByIdQueryHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<ShopDto?> Handle(GetShopByIdQuery request, CancellationToken cancellationToken)
    {
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);

        if (shop == null)
        {
            return null;
        }

        return ShopMapper.MapToDto(shop);
    }
}
