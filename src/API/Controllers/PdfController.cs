using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Pdf.DTOs;
using pos_system_api.Core.Application.Pdf.Queries.GenerateReceiptPdf;

namespace pos_system_api.API.Controllers;

/// <summary>
/// PDF generation endpoints (receipts, invoices, reports)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PdfController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IPdfService _pdfService;

    public PdfController(IMediator mediator, IPdfService pdfService)
    {
        _mediator = mediator;
        _pdfService = pdfService;
    }

    /// <summary>
    /// Generate receipt PDF for an order
    /// </summary>
    /// <param name="orderId">Order identifier - either the order Id (GUID) or OrderNumber (e.g., SO-20251105213131-5798)</param>
    /// <param name="language">Language code: en-US or ar (default: shop config or en-US)</param>
    /// <param name="paperType">Paper type: A4, A5, Thermal80mm, Thermal58mm (default: shop config or A5)</param>
    /// <returns>PDF file</returns>
    [HttpGet("receipt/{orderId}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateReceipt(
        string orderId,
        [FromQuery] string? language = null,
        [FromQuery] string? paperType = null)
    {
        var result = await _mediator.Send(new GenerateReceiptPdfQuery(orderId, language, paperType));

        if (result == null)
        {
            return NotFound(new { message = $"Order {orderId} not found" });
        }

        return File(result.PdfBytes, "application/pdf", result.FileName);
    }

    /// <summary>
    /// Generate receipt PDF from custom data (for testing or custom scenarios)
    /// </summary>
    /// <param name="receiptData">Custom receipt data</param>
    /// <returns>PDF file</returns>
    [HttpPost("receipt/custom")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateCustomReceipt([FromBody] ReceiptDto receiptData)
    {
        if (string.IsNullOrEmpty(receiptData.OrderNumber))
        {
            return BadRequest(new { message = "Order number is required" });
        }

        if (!receiptData.Items.Any())
        {
            return BadRequest(new { message = "At least one item is required" });
        }

        if (!string.IsNullOrEmpty(receiptData.LogoUrl) && !IsSafePublicUrl(receiptData.LogoUrl))
        {
            return BadRequest(new { message = "logoUrl must be an absolute http(s) URL pointing to a public host" });
        }

        var pdfBytes = await _pdfService.GenerateReceiptPdfAsync(receiptData);
        var fileName = $"Receipt_{receiptData.OrderNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    private static bool IsSafePublicUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        var host = uri.Host;
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host.Equals("metadata.google.internal", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (System.Net.IPAddress.TryParse(host, out var ip))
        {
            if (System.Net.IPAddress.IsLoopback(ip))
            {
                return false;
            }

            var bytes = ip.GetAddressBytes();
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                // 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 169.254.0.0/16, 100.64.0.0/10
                if (bytes[0] == 10) return false;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return false;
                if (bytes[0] == 192 && bytes[1] == 168) return false;
                if (bytes[0] == 169 && bytes[1] == 254) return false;
                if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return false;
            }
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal) return false;
            }
        }

        return true;
    }
}
