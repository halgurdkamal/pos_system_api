namespace pos_system_api.Core.Application.Shops.DTOs;

/// <summary>
/// Request DTO for users to create their own shop
/// Simplified compared to full shop registration
/// </summary>
public record CreateOwnShopRequestDto
{
    /// <summary>
    /// Name of the pharmacy shop
    /// </summary>
    public string ShopName { get; init; } = string.Empty;

    /// <summary>
    /// Primary phone number
    /// </summary>
    public string PhoneNumber { get; init; } = string.Empty;

    /// <summary>
    /// Primary email address
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Street address
    /// </summary>
    public string Address { get; init; } = string.Empty;

    /// <summary>
    /// City
    /// </summary>
    public string City { get; init; } = string.Empty;

    /// <summary>
    /// State/Province (optional)
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Country (optional, defaults to Iraq)
    /// </summary>
    public string? Country { get; init; }

    /// <summary>
    /// Postal/ZIP code (optional)
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Pharmacy license number (optional, can be added later)
    /// </summary>
    public string? LicenseNumber { get; init; }

    /// <summary>
    /// Tax ID/VAT number (optional)
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Shop description (optional)
    /// </summary>
    public string? Description { get; init; }
}
