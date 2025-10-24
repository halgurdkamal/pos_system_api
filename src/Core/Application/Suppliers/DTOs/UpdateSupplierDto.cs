using pos_system_api.Core.Application.Common.DTOs;

namespace pos_system_api.Core.Application.Suppliers.DTOs;

/// <summary>
/// DTO for updating an existing supplier
/// </summary>
public class UpdateSupplierDto
{
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierType { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Address
    public AddressDto Address { get; set; } = new();
    
    // Business Terms
    public string PaymentTerms { get; set; } = string.Empty;
    public int DeliveryLeadTime { get; set; }
    public decimal MinimumOrderValue { get; set; }
    
    // Status
    public bool IsActive { get; set; }
    
    // Additional Info
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Notes { get; set; }
}
