using pos_system_api.Core.Application.Common.DTOs;

namespace pos_system_api.Core.Application.Suppliers.DTOs;

/// <summary>
/// DTO for creating a new supplier
/// </summary>
public class CreateSupplierDto
{
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierType { get; set; } = "Distributor";
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Address
    public AddressDto Address { get; set; } = new();
    
    // Business Terms
    public string PaymentTerms { get; set; } = "Net 30";
    public int DeliveryLeadTime { get; set; } = 7;
    public decimal MinimumOrderValue { get; set; } = 0;
    
    // Additional Info (optional)
    public string? Website { get; set; }
    public string? TaxId { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Notes { get; set; }
}
