namespace pos_system_api.Core.Application.Common.Interfaces;

/// <summary>
/// Service for generating PDF documents (receipts, reports, invoices)
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Generate a receipt PDF for an order
    /// </summary>
    /// <param name="receiptData">Receipt data to generate PDF from</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GenerateReceiptPdfAsync(Application.Pdf.DTOs.ReceiptDto receiptData);
    
    /// <summary>
    /// Generate a receipt PDF and save to file
    /// </summary>
    /// <param name="receiptData">Receipt data</param>
    /// <param name="filePath">Path to save PDF</param>
    Task GenerateReceiptPdfToFileAsync(Application.Pdf.DTOs.ReceiptDto receiptData, string filePath);
}
