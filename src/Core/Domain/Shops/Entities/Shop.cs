using pos_system_api.Core.Domain.Common;
using pos_system_api.Core.Domain.Common.ValueObjects;
using pos_system_api.Core.Domain.Shops.ValueObjects;

namespace pos_system_api.Core.Domain.Shops.Entities;

/// <summary>
/// Represents a pharmacy shop registered in the multi-tenant POS system
/// </summary>
public class Shop : BaseEntity
{
    // Basic Information
    public string ShopName { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? VatRegistrationNumber { get; set; } // For receipts
    public string? PharmacyRegistrationNumber { get; set; } // Required on pharmacy receipts
    
    // Value Objects (configured as owned entities)
    public Address Address { get; set; } = new();
    public Contact Contact { get; set; } = new();
    
    // Branding & Visual Identity
    public string? LogoUrl { get; set; } // Shop logo for receipts and UI
    public List<string> ShopImageUrls { get; set; } = new(); // Storefront, interior photos
    public string? BrandColorPrimary { get; set; } // Hex color code (e.g., "#007BFF")
    public string? BrandColorSecondary { get; set; }
    
    // Receipt Configuration (owned entity)
    public ReceiptConfiguration ReceiptConfig { get; set; } = new();
    
    // Hardware Configuration (owned entity)
    public HardwareConfiguration HardwareConfig { get; set; } = new();
    
    // Shop Settings
    public string Currency { get; set; } = "USD";
    public decimal DefaultTaxRate { get; set; }
    public bool AutoReorderEnabled { get; set; } = true;
    public int LowStockAlertThreshold { get; set; } = 50;
    
    // Operating Hours (stored as JSON in database)
    public Dictionary<string, string> OperatingHours { get; set; } = new();
    
    // Compliance Settings
    public bool RequiresPrescriptionVerification { get; set; } = true;
    public bool AllowsControlledSubstances { get; set; } = false;
    public List<string> AcceptedInsuranceProviders { get; set; } = new(); // Insurance codes
    
    // Shop Members (Many-to-Many relationship with Users via ShopUser)
    public ICollection<ShopUser> Members { get; set; } = new List<ShopUser>();
    
    // Status
    public ShopStatus Status { get; set; } = ShopStatus.Active;
    public DateTime RegistrationDate { get; set; }

    public Shop() 
    {
        RegistrationDate = DateTime.UtcNow;
    }

    public Shop(
        string shopName,
        string legalName,
        string licenseNumber,
        Address address,
        Contact contact)
    {
        Id = $"SHOP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        ShopName = shopName;
        LegalName = legalName;
        LicenseNumber = licenseNumber;
        Address = address;
        Contact = contact;
        RegistrationDate = DateTime.UtcNow;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateShopInfo(string shopName, string legalName, Address address, Contact contact)
    {
        ShopName = shopName;
        LegalName = legalName;
        Address = address;
        Contact = contact;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateReceiptConfiguration(ReceiptConfiguration receiptConfig)
    {
        ReceiptConfig = receiptConfig;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateHardwareConfiguration(HardwareConfiguration hardwareConfig)
    {
        HardwareConfig = hardwareConfig;
        LastUpdated = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = ShopStatus.Suspended;
        LastUpdated = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = ShopStatus.Active;
        LastUpdated = DateTime.UtcNow;
    }

    public void Close()
    {
        Status = ShopStatus.Closed;
        LastUpdated = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Get all active members of this shop
    /// </summary>
    public IEnumerable<ShopUser> GetActiveMembers()
    {
        return Members.Where(m => m.IsActive);
    }
    
    /// <summary>
    /// Get all owners of this shop
    /// </summary>
    public IEnumerable<ShopUser> GetOwners()
    {
        return Members.Where(m => m.IsOwner && m.IsActive);
    }
    
    /// <summary>
    /// Check if a user is a member of this shop
    /// </summary>
    public bool HasMember(string userId)
    {
        return Members.Any(m => m.UserId == userId && m.IsActive);
    }
}

/// <summary>
/// Status of a shop in the system
/// </summary>
public enum ShopStatus
{
    Active = 0,
    Suspended = 1,
    Closed = 2
}
