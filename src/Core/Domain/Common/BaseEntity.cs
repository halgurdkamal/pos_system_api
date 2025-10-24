namespace pos_system_api.Core.Domain.Common;

/// <summary>
/// Base class for all domain entities
/// </summary>
public abstract class BaseEntity
{
    public string Id { get; protected set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public string? UpdatedBy { get; set; }
}
