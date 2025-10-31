namespace pos_system_api.Core.Domain.Shops.ValueObjects;

/// <summary>
/// Hardware configuration for POS terminals and peripherals
/// </summary>
public class HardwareConfiguration
{
    // Receipt Printer
    public string? ReceiptPrinterName { get; set; } // Windows printer name (e.g., "EPSON TM-T88V")
    public string? ReceiptPrinterConnectionType { get; set; } // "USB", "Network", "Bluetooth"
    public string? ReceiptPrinterIpAddress { get; set; } // For network printers
    public int? ReceiptPrinterPort { get; set; } = 9100; // Standard ESC/POS port

    // Barcode Label Printer
    public string? BarcodePrinterName { get; set; } // For printing shelf labels
    public string? BarcodePrinterConnectionType { get; set; }
    public string? BarcodePrinterIpAddress { get; set; }
    public BarcodeLabelSize BarcodeLabelSize { get; set; } = BarcodeLabelSize.Small; // 40x30mm, 50x25mm, etc.

    // Barcode Scanner
    public string? BarcodeScannerModel { get; set; } // "Zebra DS2208", "Honeywell 1900"
    public string? BarcodeScannerConnectionType { get; set; } // "USB", "Bluetooth"
    public bool AutoSubmitOnScan { get; set; } = true; // Auto-submit after scan

    // Cash Drawer
    public string? CashDrawerModel { get; set; }
    public bool CashDrawerEnabled { get; set; } = true;
    public string? CashDrawerOpenCommand { get; set; } // ESC/POS command to open drawer

    // Payment Terminal
    public string? PaymentTerminalModel { get; set; } // "Verifone VX520", "Ingenico iCT250"
    public string? PaymentTerminalConnectionType { get; set; } // "Serial", "USB", "Network"
    public string? PaymentTerminalIpAddress { get; set; }
    public bool IntegratedPayments { get; set; } = false; // Direct integration vs standalone

    // POS Terminal
    public string? PosTerminalId { get; set; } // Unique ID for this POS station
    public string? PosTerminalName { get; set; } // "Front Counter 1", "Drive-Thru"

    // Display Settings
    public bool CustomerDisplayEnabled { get; set; } = false; // Secondary customer-facing display
    public string? CustomerDisplayType { get; set; } // "LCD", "LED Pole Display"

    public HardwareConfiguration() { }

    public HardwareConfiguration(
        string? receiptPrinterName,
        string? barcodePrinterName = null,
        string? posTerminalId = null)
    {
        ReceiptPrinterName = receiptPrinterName;
        BarcodePrinterName = barcodePrinterName;
        PosTerminalId = posTerminalId ?? $"POS-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
    }
}

/// <summary>
/// Standard barcode label sizes for shelf labels
/// </summary>
public enum BarcodeLabelSize
{
    Small = 0,      // 40x30mm
    Medium = 1,     // 50x25mm
    Large = 2,      // 60x40mm
    Shelf = 3       // 100x50mm (shelf edge labels)
}
