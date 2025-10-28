using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Services;

public interface IEffectivePackagingService
{
    Task<EffectivePackagingDto> GetEffectivePackagingAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default);
}
