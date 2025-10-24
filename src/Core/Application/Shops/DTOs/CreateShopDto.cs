using pos_system_api.Core.Application.Common.DTOs;

namespace pos_system_api.Core.Application.Shops.DTOs;

/// <summary>
/// DTO for creating a new shop (registration)
/// </summary>
public class CreateShopDto
{
    public string ShopName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string VatRegistrationNumber { get; set; } = string.Empty;
    public string PharmacyRegistrationNumber { get; set; } = string.Empty;
    
    // Contact and Address
    public AddressDto Address { get; set; } = new();
    public ContactDto Contact { get; set; } = new();
    
    // Branding (optional)
    public string? LogoUrl { get; set; }
    public List<string>? ShopImageUrls { get; set; }
    public string? BrandColorPrimary { get; set; }
    public string? BrandColorSecondary { get; set; }
    
    // Configuration (optional, can use defaults)
    public ReceiptConfigurationDto? ReceiptConfig { get; set; }
    public HardwareConfigurationDto? HardwareConfig { get; set; }
    
    // Operating Schedule (optional)
    public Dictionary<string, string>? OperatingHours { get; set; }
    
    // Compliance
    public bool RequiresPrescriptionVerification { get; set; } = true;
    public bool AllowsControlledSubstances { get; set; } = false;
    public List<string>? AcceptedInsuranceProviders { get; set; }
}
