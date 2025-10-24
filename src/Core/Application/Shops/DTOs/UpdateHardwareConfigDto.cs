namespace pos_system_api.Core.Application.Shops.DTOs;

/// <summary>
/// DTO for updating hardware configuration only
/// </summary>
public class UpdateHardwareConfigDto
{
    // Receipt Printer
    public string ReceiptPrinterName { get; set; } = string.Empty;
    public string ReceiptPrinterConnection { get; set; } = "USB";
    public string? ReceiptPrinterIpAddress { get; set; }
    public int? ReceiptPrinterPort { get; set; }

    // Barcode Printer
    public string? BarcodePrinterName { get; set; }
    public string BarcodePrinterConnection { get; set; } = "USB";
    public string? BarcodePrinterIpAddress { get; set; }
    public string BarcodeLabelSize { get; set; } = "Small";

    // Barcode Scanner
    public string? BarcodeScannerModel { get; set; }
    public string BarcodeScannerConnection { get; set; } = "USB";

    // Cash Drawer
    public string? CashDrawerModel { get; set; }
    public bool CashDrawerEnabled { get; set; } = true;
    public string CashDrawerOpenCommand { get; set; } = string.Empty;

    // Payment Terminal
    public string? PaymentTerminalModel { get; set; }
    public string PaymentTerminalConnection { get; set; } = "Serial";
    public string? PaymentTerminalIpAddress { get; set; }

    // POS Terminal Info
    public string PosTerminalId { get; set; } = string.Empty;
    public string PosTerminalName { get; set; } = string.Empty;

    // Customer Display
    public bool CustomerDisplayEnabled { get; set; } = false;
    public string CustomerDisplayType { get; set; } = "LED";
}
