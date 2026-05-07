namespace pos_system_api.Core.Application.Pdf.DTOs;

/// <summary>
/// Paper type for receipt printing
/// </summary>
public enum PaperType
{
    /// <summary>A4 paper (210 x 297 mm) - Standard document size</summary>
    A4,
    
    /// <summary>A5 paper (148 x 210 mm) - Half of A4, good for receipts</summary>
    A5,
    
    /// <summary>Thermal printer 80mm (80 x continuous mm) - Standard thermal receipt</summary>
    Thermal80mm,
    
    /// <summary>Thermal printer 58mm (58 x continuous mm) - Compact thermal receipt</summary>
    Thermal58mm
}

/// <summary>
/// Receipt data for PDF generation
/// </summary>
public record ReceiptDto
{
    public string OrderNumber { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public string? ShopAddress { get; init; }
    public string? ShopPhone { get; init; }
    public string? ShopEmail { get; init; }
    public string? LogoUrl { get; init; }
    public string? VatRegistrationNumber { get; init; }
    public string? PharmacyLicenseNumber { get; init; }
    
    // Receipt Configuration
    public string? HeaderText { get; init; }
    public string? FooterText { get; init; }
    public bool ShowLogo { get; init; } = true;
    public bool ShowQrCode { get; init; } = false;
    public bool ShowTaxBreakdown { get; init; } = true;
    public bool ShowVatNumber { get; init; } = true;
    public bool ShowPharmacyLicense { get; init; } = true;
    public string Language { get; init; } = "en-US"; // en-US or ar
    public PaperType PaperType { get; init; } = PaperType.A5; // Paper size for receipt
    
    // Order Information
    public DateTime OrderDate { get; init; }
    public string? CustomerName { get; init; }
    public string? SalespersonName { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    
    // Items
    public List<ReceiptItemDto> Items { get; init; } = new();
    
    // Totals
    public decimal Subtotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal Total { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal Change { get; init; }
    public string Currency { get; init; } = "IQD";
}

/// <summary>
/// Receipt item data
/// </summary>
public record ReceiptItemDto
{
    public string ItemName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Total { get; init; }
    public bool RequiresPrescription { get; init; }
}
