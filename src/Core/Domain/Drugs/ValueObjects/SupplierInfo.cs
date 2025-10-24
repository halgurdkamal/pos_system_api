namespace pos_system_api.Core.Domain.Drugs.ValueObjects;

/// <summary>
/// Represents supplier information for a drug
/// </summary>
public class SupplierInfo
{
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ContactNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
