using pos_system_api.Core.Domain.Common;
using pos_system_api.Core.Domain.Common.ValueObjects;

namespace pos_system_api.Core.Domain.Suppliers.Entities;

/// <summary>
/// Represents a drug supplier (manufacturer, distributor, or wholesaler)
/// </summary>
public class Supplier : BaseEntity
{
    public string SupplierName { get; set; } = string.Empty;
    public SupplierType SupplierType { get; set; }
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Value Object (will be configured as owned entity)
    public Address Address { get; set; } = new();

    // Business Terms
    public string PaymentTerms { get; set; } = "Net 30"; // e.g., "Net 30", "Net 60", "COD"
    public int DeliveryLeadTime { get; set; } // In days
    public decimal MinimumOrderValue { get; set; }

    // Status
    public bool IsActive { get; set; } = true;

    // Additional Info
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public string? LicenseNumber { get; set; }

    public Supplier() { }

    public Supplier(
        string supplierName,
        SupplierType supplierType,
        string contactNumber,
        string email,
        Address address)
    {
        Id = $"SUP-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        SupplierName = supplierName;
        SupplierType = supplierType;
        ContactNumber = contactNumber;
        Email = email;
        Address = address;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        LastUpdated = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        LastUpdated = DateTime.UtcNow;
    }

    public void UpdateContactInfo(string contactNumber, string email)
    {
        ContactNumber = contactNumber;
        Email = email;
        LastUpdated = DateTime.UtcNow;
    }
}

/// <summary>
/// Type of supplier
/// </summary>
public enum SupplierType
{
    Manufacturer = 0,
    Distributor = 1,
    Wholesaler = 2,
    LocalAgent = 3
}
