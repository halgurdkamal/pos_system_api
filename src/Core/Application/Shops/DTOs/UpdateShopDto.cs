using pos_system_api.Core.Application.Common.DTOs;

namespace pos_system_api.Core.Application.Shops.DTOs;

/// <summary>
/// DTO for updating an existing shop
/// </summary>
public class UpdateShopDto
{
    public string ShopName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string VatRegistrationNumber { get; set; } = string.Empty;
    public string PharmacyRegistrationNumber { get; set; } = string.Empty;

    // Contact and Address
    public AddressDto Address { get; set; } = new();
    public ContactDto Contact { get; set; } = new();

    // Branding
    public string? LogoUrl { get; set; }
    public List<string>? ShopImageUrls { get; set; }
    public string? BrandColorPrimary { get; set; }
    public string? BrandColorSecondary { get; set; }

    // Operating Schedule
    public Dictionary<string, string>? OperatingHours { get; set; }

    // Compliance
    public bool RequiresPrescriptionVerification { get; set; }
    public bool AllowsControlledSubstances { get; set; }
    public List<string>? AcceptedInsuranceProviders { get; set; }
}
