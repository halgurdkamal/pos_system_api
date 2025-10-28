using MediatR;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Services;

namespace pos_system_api.Core.Application.Inventory.Queries.GetEffectivePackaging;

public record GetEffectivePackagingQuery(string ShopId, string DrugId) : IRequest<EffectivePackagingDto>;

public class GetEffectivePackagingQueryHandler : IRequestHandler<GetEffectivePackagingQuery, EffectivePackagingDto>
{
    private readonly IEffectivePackagingService _packagingService;

    public GetEffectivePackagingQueryHandler(IEffectivePackagingService packagingService)
    {
        _packagingService = packagingService;
    }

    public async Task<EffectivePackagingDto> Handle(GetEffectivePackagingQuery request, CancellationToken cancellationToken)
    {
        return await _packagingService.GetEffectivePackagingAsync(request.ShopId, request.DrugId, cancellationToken);
    }
}
