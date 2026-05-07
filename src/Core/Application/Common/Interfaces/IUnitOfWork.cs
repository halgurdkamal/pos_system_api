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
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persist all pending changes. Returns the number of rows affected.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
