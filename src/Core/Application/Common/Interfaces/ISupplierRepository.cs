using pos_system_api.Core.Domain.Suppliers.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Supplier entity operations
/// </summary>
public interface ISupplierRepository
{
    /// <summary>
    /// Get a supplier by its ID
    /// </summary>
    Task<Supplier?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all suppliers with pagination
    /// </summary>
    Task<(IEnumerable<Supplier> Suppliers, int TotalCount)> GetAllAsync(
        int page,
        int limit,
        bool? isActive = null,
        SupplierType? supplierType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search suppliers by name
    /// </summary>
    Task<IEnumerable<Supplier>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active suppliers only
    /// </summary>
    Task<IEnumerable<Supplier>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new supplier
    /// </summary>
    Task<Supplier> AddAsync(Supplier supplier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing supplier
    /// </summary>
    Task<Supplier> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a supplier
    /// </summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an email already exists
    /// </summary>
    Task<bool> EmailExistsAsync(
        string email,
        string? excludeSupplierId = null,
        CancellationToken cancellationToken = default);
}
