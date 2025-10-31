namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Service interface for barcode and QR code generation
/// </summary>
public interface IBarcodeService
{
    /// <summary>
    /// Generate a barcode image from text/number
    /// </summary>
    byte[] GenerateBarcode(string data, int width = 300, int height = 100);

    /// <summary>
    /// Generate a QR code image from text/data
    /// </summary>
    byte[] GenerateQRCode(string data, int width = 300, int height = 300);

    /// <summary>
    /// Decode/scan a barcode or QR code from image bytes
    /// </summary>
    string? DecodeBarcode(byte[] imageData);
}
