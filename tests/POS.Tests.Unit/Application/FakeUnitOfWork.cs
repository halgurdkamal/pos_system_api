using pos_system_api.Core.Application.Common.Interfaces;

namespace POS.Tests.Unit.Application;

/// <summary>
/// In-memory IUnitOfWork stub for handler unit tests. Counts SaveChangesAsync
/// invocations so tests can assert the handler committed exactly once.
/// Also tracks transaction lifecycle (Begin/Commit/Rollback) for Q-6 tests.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCount { get; private set; }
    public int BeginTransactionCount { get; private set; }
    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCount++;
        return Task.FromResult(0);
    }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        BeginTransactionCount++;
        return Task.FromResult<IUnitOfWorkTransaction>(new FakeTransaction(this));
    }

    private sealed class FakeTransaction : IUnitOfWorkTransaction
    {
        private readonly FakeUnitOfWork _owner;
        private bool _committed;

        public FakeTransaction(FakeUnitOfWork owner) => _owner = owner;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            _owner.CommitCount++;
            _committed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_committed)
            {
                _owner.RollbackCount++;
            }
            return ValueTask.CompletedTask;
        }
    }
}
