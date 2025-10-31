namespace pos_system_api.Core.Domain.Shops.ValueObjects;

/// <summary>
/// Configuration for receipt printing and branding
/// </summary>
public class ReceiptConfiguration
{
    // Receipt Branding
    public string ReceiptShopName { get; set; } = string.Empty; // Name displayed on receipt (can differ from legal name)
    public string? ReceiptHeaderText { get; set; } // Custom header text
    public string? ReceiptFooterText { get; set; } // "Thank you for your purchase!"
    public string? ReturnPolicyText { get; set; } // Return policy on receipt
    public string? PharmacistName { get; set; } // Pharmacist name for prescription receipts

    // Receipt Content Settings
    public bool ShowLogoOnReceipt { get; set; } = true;
    public bool ShowTaxBreakdown { get; set; } = true;
    public bool ShowBarcode { get; set; } = true; // Receipt barcode for returns
    public bool ShowQrCode { get; set; } = false; // QR code for digital receipt
    public bool ShowPharmacyLicense { get; set; } = true;
    public bool ShowVatNumber { get; set; } = true;

    // Receipt Format
    public int ReceiptWidth { get; set; } = 80; // 80mm thermal paper (standard)
    public string ReceiptLanguage { get; set; } = "en-US";
    public bool PrintDuplicateReceipt { get; set; } = false; // Auto-print duplicate

    // Regulatory Compliance
    public string? PharmacyWarningText { get; set; } // "Keep out of reach of children"
    public string? ControlledSubstanceWarning { get; set; } // For Schedule drugs

    public ReceiptConfiguration() { }

    public ReceiptConfiguration(
        string receiptShopName,
        string? headerText = null,
        string? footerText = null)
    {
        ReceiptShopName = receiptShopName;
        ReceiptHeaderText = headerText;
        ReceiptFooterText = footerText;
    }
}
