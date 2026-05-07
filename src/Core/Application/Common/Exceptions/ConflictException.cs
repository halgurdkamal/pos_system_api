namespace pos_system_api.Core.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a request would create or modify state that conflicts
/// with an existing resource (e.g. duplicate unique key, optimistic-concurrency
/// failure). Maps to HTTP 409 Conflict via GlobalExceptionHandlingMiddleware.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException) { }
}
