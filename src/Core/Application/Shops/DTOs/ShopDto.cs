using pos_system_api.Core.Application.Common.DTOs;

namespace pos_system_api.Core.Application.Shops.DTOs;

/// <summary>
/// DTO for Shop entity (API response)
/// </summary>
public class ShopDto
{
    public string Id { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string VatRegistrationNumber { get; set; } = string.Empty;
    public string PharmacyRegistrationNumber { get; set; } = string.Empty;
    
    // Contact and Address
    public AddressDto Address { get; set; } = new();
    public ContactDto Contact { get; set; } = new();
    
    // Branding
    public string? LogoUrl { get; set; }
    public List<string> ShopImageUrls { get; set; } = new();
    public string? BrandColorPrimary { get; set; }
    public string? BrandColorSecondary { get; set; }
    
    // Configuration
    public ReceiptConfigurationDto ReceiptConfig { get; set; } = new();
    public HardwareConfigurationDto HardwareConfig { get; set; } = new();
    
    // Operating Schedule
    public Dictionary<string, string> OperatingHours { get; set; } = new();
    
    // Compliance
    public bool RequiresPrescriptionVerification { get; set; }
    public bool AllowsControlledSubstances { get; set; }
    public List<string> AcceptedInsuranceProviders { get; set; } = new();
    
    // Status
    public string Status { get; set; } = "Active";
    public DateTime RegistrationDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
