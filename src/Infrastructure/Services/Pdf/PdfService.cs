using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Pdf.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace pos_system_api.Infrastructure.Services.Pdf;

/// <summary>
/// PDF generation service using QuestPDF
/// Supports Arabic text, RTL layout, and custom branding
/// </summary>
public class PdfService : IPdfService
{
    private readonly ILogger<PdfService> _logger;

    public PdfService(ILogger<PdfService> logger)
    {
        _logger = logger;

        // Configure QuestPDF license (Community license for open-source/small businesses)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(ReceiptDto receiptData)
    {
        return await Task.Run(() =>
        {
            var document = CreateReceiptDocument(receiptData);
            return document.GeneratePdf();
        });
    }

    public async Task GenerateReceiptPdfToFileAsync(ReceiptDto receiptData, string filePath)
    {
        await Task.Run(() =>
        {
            var document = CreateReceiptDocument(receiptData);
            document.GeneratePdf(filePath);
        });
    }

    private IDocument CreateReceiptDocument(ReceiptDto receipt)
    {
        bool isArabic = receipt.Language.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        bool useArabicDigits = isArabic;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                // Set page size based on paper type
                var pageSize = GetPageSize(receipt.PaperType);
                page.Size(pageSize);

                // Set margins based on paper type
                var margin = GetMargin(receipt.PaperType);
                page.Margin(margin);

                // Set content direction based on language
                if (isArabic)
                {
                    page.ContentFromRightToLeft();
                }

                page.Header().Element(c => ComposeHeader(c, receipt, isArabic));
                page.Content().Element(c => ComposeContent(c, receipt, isArabic, useArabicDigits));
                page.Footer().Element(c => ComposeFooter(c, receipt, isArabic));
            });
        });
    }

    private PageSize GetPageSize(pos_system_api.Core.Application.Pdf.DTOs.PaperType paperType)
    {
        return paperType switch
        {
            pos_system_api.Core.Application.Pdf.DTOs.PaperType.A4 => PageSizes.A4,
            pos_system_api.Core.Application.Pdf.DTOs.PaperType.A5 => PageSizes.A5,
            pos_system_api.Core.Application.Pdf.DTOs.PaperType.Thermal80mm => new PageSize(
                226.77f,
                566.93f
            ), // 80mm x 200mm in points
            pos_system_api.Core.Application.Pdf.DTOs.PaperType.Thermal58mm => new PageSize(
                164.41f,
                566.93f
            ), // 58mm x 200mm in points
            _ => PageSizes.A5,
        };
    }

    private float GetMargin(pos_system_api.Core.Application.Pdf.DTOs.PaperType paperType)
    {
        return paperType switch
        {
            pos_system_api.Core.Application.Pdf.DTOs.PaperType.A4 => 20f,
            pos_system_api.Core.Application.Pdf.DTOs.PaperType.A5 => 15f,
            pos_system_api.Core.Application.Pdf.DTOs.PaperType.Thermal80mm => 5f,
            pos_system_api.Core.Application.Pdf.DTOs.PaperType.Thermal58mm => 3f,
            _ => 15f,
        };
    }

    private void ComposeHeader(IContainer container, ReceiptDto receipt, bool isArabic)
    {
        container.Column(column =>
        {
            column.Spacing(5);

            // Logo (if enabled and URL provided)
            if (receipt.ShowLogo && !string.IsNullOrEmpty(receipt.LogoUrl))
            {
                column
                    .Item()
                    .AlignCenter()
                    .Height(60)
                    .Container()
                    .Border(0)
                    .Text("[LOGO]")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Medium);
                // Note: QuestPDF supports images, but needs local file or byte array
                // In production, download the image from URL first
            }

            // Shop Name
            column.Item().AlignCenter().Text(receipt.ShopName).FontSize(16).Bold();

            // Divider
            column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // Order Info Section - Each on new line
            column
                .Item()
                .Column(c =>
                {
                    // Order Number
                    var orderLabel = isArabic ? "رقم الطلب:" : "Order Number:";
                    c.Item()
                        .Text(text =>
                        {
                            text.Span($"{orderLabel} ").FontSize(9).Bold();
                            text.Span(receipt.OrderNumber).FontSize(9);
                        });

                    // Date
                    var dateLabel = isArabic ? "التاريخ:" : "Date:";
                    var formattedDate = receipt.OrderDate.ToString("MMM d, yyyy h:mm tt");
                    if (isArabic)
                    {
                        formattedDate = ArabicNumberFormatter.ToArabicDigits(formattedDate);
                    }
                    c.Item()
                        .Text(text =>
                        {
                            text.Span($"{dateLabel} ").FontSize(9).Bold();
                            text.Span(formattedDate).FontSize(9);
                        });

                    // Customer Name (if available)
                    if (!string.IsNullOrEmpty(receipt.CustomerName))
                    {
                        var customerLabel = isArabic ? "العميل:" : "Customer:";
                        c.Item()
                            .Text(text =>
                            {
                                text.Span($"{customerLabel} ").FontSize(9).Bold();
                                text.Span(receipt.CustomerName).FontSize(9);
                            });
                    }

                    // Salesperson (if available)
                    if (!string.IsNullOrEmpty(receipt.SalespersonName))
                    {
                        var salesLabel = isArabic ? "البائع:" : "Salesperson:";
                        c.Item()
                            .Text(text =>
                            {
                                text.Span($"{salesLabel} ").FontSize(9).Bold();
                                text.Span(receipt.SalespersonName).FontSize(9);
                            });
                    }

                    // Payment Method
                    var paymentLabel = isArabic ? "طريقة الدفع:" : "Payment:";
                    c.Item()
                        .Text(text =>
                        {
                            text.Span($"{paymentLabel} ").FontSize(9).Bold();
                            text.Span(receipt.PaymentMethod).FontSize(9);
                        });
                });

            column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private void ComposeContent(
        IContainer container,
        ReceiptDto receipt,
        bool isArabic,
        bool useArabicDigits
    )
    {
        container.Column(column =>
        {
            column.Spacing(5);

            // Items Table Header
            column
                .Item()
                .Row(row =>
                {
                    row.RelativeItem(4).Text(isArabic ? "المنتج" : "Item").FontSize(10).Bold();
                    row.RelativeItem(1)
                        .AlignCenter()
                        .Text(isArabic ? "الكمية" : "Qty")
                        .FontSize(10)
                        .Bold();
                    row.RelativeItem(2)
                        .AlignRight()
                        .Text(isArabic ? "المجموع" : "Total")
                        .FontSize(10)
                        .Bold();
                });

            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // Items
            foreach (var item in receipt.Items)
            {
                column
                    .Item()
                    .Row(row =>
                    {
                        row.RelativeItem(4)
                            .Column(c =>
                            {
                                c.Item().Text(item.ItemName).FontSize(9);
                                if (item.RequiresPrescription)
                                {
                                    var rxText = isArabic
                                        ? "℞ يتطلب وصفة طبية"
                                        : "℞ Prescription Required";
                                    c.Item().Text(rxText).FontSize(7).FontColor(Colors.Red.Medium);
                                }
                            });

                        var qtyText = useArabicDigits
                            ? ArabicNumberFormatter.ToArabicDigits(item.Quantity.ToString())
                            : item.Quantity.ToString();
                        row.RelativeItem(1).AlignCenter().Text(qtyText).FontSize(9);

                        var totalText = ArabicNumberFormatter.FormatCurrency(
                            item.Total,
                            receipt.Currency,
                            useArabicDigits
                        );
                        row.RelativeItem(2).AlignRight().Text(totalText).FontSize(9);
                    });

                column
                    .Item()
                    .PaddingVertical(2)
                    .LineHorizontal(0.5f)
                    .LineColor(Colors.Grey.Lighten2);
            }

            column.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // Totals Section
            column
                .Item()
                .PaddingTop(10)
                .Column(totalsColumn =>
                {
                    // Subtotal
                    totalsColumn
                        .Item()
                        .Row(row =>
                        {
                            var subtotalLabel = isArabic ? "المجموع الفرعي:" : "Subtotal:";
                            row.RelativeItem().Text(subtotalLabel).FontSize(10);
                            row.ConstantItem(120)
                                .AlignRight()
                                .Text(
                                    ArabicNumberFormatter.FormatCurrency(
                                        receipt.Subtotal,
                                        receipt.Currency,
                                        useArabicDigits
                                    )
                                )
                                .FontSize(10);
                        });

                    // Tax (if enabled)
                    if (receipt.ShowTaxBreakdown && receipt.TaxAmount > 0)
                    {
                        var taxLabel = isArabic
                            ? $"الضريبة ({ArabicNumberFormatter.FormatNumber(receipt.TaxRate, 2, useArabicDigits)}%):"
                            : $"Tax ({receipt.TaxRate:0.##}%):";

                        totalsColumn
                            .Item()
                            .Row(row =>
                            {
                                row.RelativeItem().Text(taxLabel).FontSize(10);
                                row.ConstantItem(120)
                                    .AlignRight()
                                    .Text(
                                        ArabicNumberFormatter.FormatCurrency(
                                            receipt.TaxAmount,
                                            receipt.Currency,
                                            useArabicDigits
                                        )
                                    )
                                    .FontSize(10);
                            });
                    }

                    // Total
                    totalsColumn.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Black);
                    totalsColumn
                        .Item()
                        .PaddingTop(5)
                        .Row(row =>
                        {
                            var totalLabel = isArabic ? "الإجمالي:" : "Total:";
                            row.RelativeItem().Text(totalLabel).FontSize(12).Bold();
                            row.ConstantItem(120)
                                .AlignRight()
                                .Text(
                                    ArabicNumberFormatter.FormatCurrency(
                                        receipt.Total,
                                        receipt.Currency,
                                        useArabicDigits
                                    )
                                )
                                .FontSize(12)
                                .Bold();
                        });

                    // Amount Paid
                    totalsColumn
                        .Item()
                        .PaddingTop(5)
                        .Row(row =>
                        {
                            var paidLabel = isArabic ? "المبلغ المدفوع:" : "Amount Paid:";
                            row.RelativeItem().Text(paidLabel).FontSize(10);
                            row.ConstantItem(120)
                                .AlignRight()
                                .Text(
                                    ArabicNumberFormatter.FormatCurrency(
                                        receipt.AmountPaid,
                                        receipt.Currency,
                                        useArabicDigits
                                    )
                                )
                                .FontSize(10);
                        });

                    // Change
                    if (receipt.Change > 0)
                    {
                        totalsColumn
                            .Item()
                            .Row(row =>
                            {
                                var changeLabel = isArabic ? "الباقي:" : "Change:";
                                row.RelativeItem().Text(changeLabel).FontSize(10);
                                row.ConstantItem(120)
                                    .AlignRight()
                                    .Text(
                                        ArabicNumberFormatter.FormatCurrency(
                                            receipt.Change,
                                            receipt.Currency,
                                            useArabicDigits
                                        )
                                    )
                                    .FontSize(10);
                            });
                    }
                });
        });
    }

    private void ComposeFooter(IContainer container, ReceiptDto receipt, bool isArabic)
    {
        container.Column(column =>
        {
            column.Spacing(5);

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);

            // QR Code (if enabled)
            if (receipt.ShowQrCode)
            {
                column
                    .Item()
                    .AlignCenter()
                    .Height(80)
                    .Container()
                    .Border(0)
                    .Text("[QR CODE]")
                    .FontSize(10)
                    .FontColor(Colors.Grey.Medium);
                // Note: QuestPDF supports QR codes, implement as needed
                column
                    .Item()
                    .AlignCenter()
                    .Text("Scan for details")
                    .FontSize(7)
                    .FontColor(Colors.Grey.Medium);
            }

            // Custom Footer Text (only if provided)
            if (!string.IsNullOrEmpty(receipt.FooterText))
            {
                column
                    .Item()
                    .AlignCenter()
                    .Text(receipt.FooterText)
                    .FontSize(10)
                    .Italic()
                    .FontColor(Colors.Grey.Darken1);
            }

            // Default thank you message
            var thankYouText = isArabic ? "شكراً لزيارتكم!" : "Thank you for your purchase!";

            column
                .Item()
                .AlignCenter()
                .Text(thankYouText)
                .FontSize(11)
                .Bold()
                .FontColor(Colors.Blue.Medium);

            var visitAgainText = isArabic ? "نتطلع لرؤيتكم مرة أخرى" : "Please come again";

            column
                .Item()
                .AlignCenter()
                .Text(visitAgainText)
                .FontSize(9)
                .FontColor(Colors.Grey.Darken1);
        });
    }
}
