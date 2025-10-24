using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Shops.Commands;
using pos_system_api.Core.Application.Shops.DTOs;

namespace pos_system_api.Core.Application.Shops.Queries.GetAllShops;

/// <summary>
/// Handler for GetAllShopsQuery
/// </summary>
public class GetAllShopsQueryHandler : IRequestHandler<GetAllShopsQuery, PagedResult<ShopDto>>
{
    private readonly IShopRepository _shopRepository;

    public GetAllShopsQueryHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<PagedResult<ShopDto>> Handle(GetAllShopsQuery request, CancellationToken cancellationToken)
    {
        var (shops, totalCount) = await _shopRepository.GetAllAsync(
            request.Page, 
            request.Limit, 
            request.Status, 
            cancellationToken
        );

        var shopDtos = shops.Select(ShopMapper.MapToDto).ToList();

        return new PagedResult<ShopDto>(shopDtos, request.Page, request.Limit, totalCount);
    }
}
