using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Application.Common.Models;

namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Drug entity operations
/// </summary>
public interface IDrugRepository
{
    Task<Drug?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-fetch drugs by their natural <see cref="Drug.DrugId"/> values in a single query.
    /// Used to avoid N+1 patterns when assembling multi-drug projections (e.g. effective packaging
    /// for an inventory page). Missing IDs are silently skipped.
    /// </summary>
    Task<IReadOnlyList<Drug>> GetByIdsAsync(
        IReadOnlyCollection<string> drugIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a drug by its barcode
    /// </summary>
    Task<Drug?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get drugs with pagination
    /// </summary>
    Task<PagedResult<Drug>> GetAllAsync(int page, int limit, CancellationToken cancellationToken = default);
    Task<Drug> CreateAsync(Drug drug, CancellationToken cancellationToken = default);
    Task<Drug> UpdateAsync(Drug drug, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string drugId, CancellationToken cancellationToken = default);
}
