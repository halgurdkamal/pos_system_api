using MediatR;

namespace pos_system_api.Core.Application.Pdf.Queries.GenerateReceiptPdf;

/// <summary>
/// Generate a receipt PDF for an existing sales order. The identifier
/// can be either the order's Id (GUID) or its OrderNumber (SO-...).
/// Returns null if the order (or its shop) cannot be found.
/// </summary>
public record GenerateReceiptPdfQuery(
    string OrderIdentifier,
    string? Language,
    string? PaperType
) : IRequest<GenerateReceiptPdfResult?>;

/// <summary>
/// Result of a receipt PDF generation: the bytes plus a suggested filename for download.
/// </summary>
public record GenerateReceiptPdfResult(byte[] PdfBytes, string FileName);
