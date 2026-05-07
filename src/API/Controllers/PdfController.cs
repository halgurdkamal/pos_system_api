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
    /// <param name="orderId">Order number (e.g., SO-20251105213131-5798)</param>
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
    [AllowAnonymous] // For testing purposes
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

        var pdfBytes = await _pdfService.GenerateReceiptPdfAsync(receiptData);
        var fileName = $"Receipt_{receiptData.OrderNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
}
