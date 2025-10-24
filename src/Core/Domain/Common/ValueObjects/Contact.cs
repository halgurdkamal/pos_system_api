namespace pos_system_api.Core.Domain.Common.ValueObjects;

/// <summary>
/// Value object representing contact information
/// </summary>
public class Contact
{
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Website { get; set; }

    public Contact() { }

    public Contact(string phone, string email, string? website = null)
    {
        Phone = phone;
        Email = email;
        Website = website;
    }
}
