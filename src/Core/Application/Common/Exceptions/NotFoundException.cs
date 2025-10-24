namespace pos_system_api.Core.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a requested entity is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public NotFoundException(string entityName, object key) 
        : base($"{entityName} with key '{key}' was not found")
    {
    }
}
