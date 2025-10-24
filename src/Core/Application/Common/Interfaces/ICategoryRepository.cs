using pos_system_api.Core.Domain.Categories.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Category entity
/// </summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(string categoryId, CancellationToken cancellationToken = default);
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetAllAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<Category> AddAsync(Category category, CancellationToken cancellationToken = default);
    Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string categoryId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string categoryId, CancellationToken cancellationToken = default);
    Task<int> GetDrugCountAsync(string categoryId, CancellationToken cancellationToken = default);
}
