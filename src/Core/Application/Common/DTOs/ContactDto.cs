namespace pos_system_api.Core.Application.Common.DTOs;

/// <summary>
/// DTO for Contact value object
/// </summary>
public class ContactDto
{
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Website { get; set; }
}
