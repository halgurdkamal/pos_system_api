using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Services;

public interface IEffectivePackagingService
{
    Task<EffectivePackagingDto> GetEffectivePackagingAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch version: resolves effective packaging for many drugs in a single shop using
    /// a constant number of database round-trips regardless of <paramref name="drugIds"/>
    /// size. Use this when projecting an inventory list page or any other multi-drug
    /// surface to avoid N+1 query patterns. The returned dictionary is keyed by
    /// <c>DrugId</c>; drugs that don't exist are silently omitted.
    /// </summary>
    Task<IReadOnlyDictionary<string, EffectivePackagingDto>> GetEffectivePackagingBatchAsync(
        string shopId,
        IReadOnlyCollection<string> drugIds,
        CancellationToken cancellationToken = default);
}
