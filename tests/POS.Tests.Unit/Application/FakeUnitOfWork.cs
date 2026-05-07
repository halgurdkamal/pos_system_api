using pos_system_api.Core.Application.Common.Interfaces;

namespace POS.Tests.Unit.Application;

/// <summary>
/// In-memory IUnitOfWork stub for handler unit tests. Counts SaveChangesAsync
/// invocations so tests can assert the handler committed exactly once.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCount++;
        return Task.FromResult(0);
    }
}
