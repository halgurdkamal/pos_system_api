namespace pos_system_api.Core.Application.Shops.DTOs;

/// <summary>
/// DTO for Receipt Configuration
/// </summary>
public class ReceiptConfigurationDto
{
    public string ReceiptShopName { get; set; } = string.Empty;
    public string HeaderText { get; set; } = string.Empty;
    public string FooterText { get; set; } = string.Empty;
    public string ReturnPolicyText { get; set; } = string.Empty;
    public string PharmacistName { get; set; } = string.Empty;
    public bool ShowLogoOnReceipt { get; set; } = true;
    public bool ShowTaxBreakdown { get; set; } = true;
    public bool ShowBarcode { get; set; } = true;
    public bool ShowQrCode { get; set; } = false;
    public int ReceiptWidth { get; set; } = 80;
    public string ReceiptLanguage { get; set; } = "en";
    public string PharmacyWarningText { get; set; } = string.Empty;
}
