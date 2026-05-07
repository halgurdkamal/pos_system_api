using pos_system_api.Core.Application.Common.Interfaces;

namespace pos_system_api.Infrastructure.Data;

/// <summary>
/// Default <see cref="IUnitOfWork"/> implementation: thin wrapper around the
/// scoped <see cref="ApplicationDbContext"/>. Both share the same DI scope
/// (Scoped lifetime) so repositories and the unit of work always see the
/// same change tracker and the same connection.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
