namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Single point of persistence for command handlers. Repositories should only
/// stage changes (Add / Update / Remove against the change tracker); the handler
/// commits them by calling <see cref="SaveChangesAsync"/> once at the end.
///
/// EF Core auto-wraps a single <c>SaveChangesAsync</c> call in a database
/// transaction, so this also gives every command implicit atomicity: every
/// row written during a single handler invocation either lands together or
/// not at all.
///
/// For commands that need pessimistic row locks (e.g. <c>SELECT ... FOR UPDATE</c>
/// to serialise concurrent writers — see Q-6), open an explicit transaction with
/// <see cref="BeginTransactionAsync"/>, run the locking read + write inside it,
/// then call <see cref="IUnitOfWorkTransaction.CommitAsync"/>. Disposing without
/// committing rolls back.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persist all pending changes. Returns the number of rows affected.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begin an explicit database transaction on the current scope's connection.
    /// Use when a command needs row-level locks (e.g. <c>FOR UPDATE</c>) that must
    /// be held across multiple round-trips before commit.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Handle to an open database transaction. Always wrap in <c>await using</c>;
/// disposing without an explicit <see cref="CommitAsync"/> rolls back.
/// </summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
