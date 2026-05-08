using Microsoft.EntityFrameworkCore.Storage;
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

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransactionAdapter(tx);
    }

    private sealed class EfTransactionAdapter : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _tx;
        private bool _committed;

        public EfTransactionAdapter(IDbContextTransaction tx) => _tx = tx;

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _tx.CommitAsync(cancellationToken);
            _committed = true;
        }

        public async ValueTask DisposeAsync()
        {
            if (!_committed)
            {
                // Best-effort rollback. EF will also roll back implicitly on
                // dispose, but being explicit keeps log output clear when an
                // exception unwinds the using-block.
                try { await _tx.RollbackAsync(); } catch { /* swallow */ }
            }
            await _tx.DisposeAsync();
        }
    }
}
