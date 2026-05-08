using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Pdf.DTOs;
using pos_system_api.Core.Domain.Sales.Entities;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.Core.Application.Pdf.Queries.GenerateReceiptPdf;

public class GenerateReceiptPdfQueryHandler
    : IRequestHandler<GenerateReceiptPdfQuery, GenerateReceiptPdfResult?>
{
    private readonly ApplicationDbContext _context;
    private readonly IPdfService _pdfService;
    private readonly ILogger<GenerateReceiptPdfQueryHandler> _logger;

    public GenerateReceiptPdfQueryHandler(
        ApplicationDbContext context,
        IPdfService pdfService,
        ILogger<GenerateReceiptPdfQueryHandler> logger)
    {
        _context = context;
        _pdfService = pdfService;
        _logger = logger;
    }

    public async Task<GenerateReceiptPdfResult?> Handle(
        GenerateReceiptPdfQuery request,
        CancellationToken cancellationToken)
    {
        var identifier = request.OrderIdentifier;
        var order = await _context.SalesOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(
                o => o.Id == identifier || o.OrderNumber == identifier,
                cancellationToken);

        if (order == null)
        {
            _logger.LogInformation("Receipt requested for unknown order {OrderIdentifier}", identifier);
            return null;
        }

        var shop = await _context.Shops
            .FirstOrDefaultAsync(s => s.Id == order.ShopId, cancellationToken);

        if (shop == null)
        {
            _logger.LogWarning(
                "Order {OrderNumber} references shop {ShopId} which no longer exists",
                order.OrderNumber, order.ShopId);
            return null;
        }

        var paperType = ResolvePaperType(request.PaperType, shop.ReceiptConfig?.PaperType);
        var language = ResolveLanguage(request.Language, shop.ReceiptConfig?.ReceiptLanguage);
        var cashierName = await GetCashierNameAsync(order.CashierId, cancellationToken);
        var items = await GetOrderItemsWithNamesAsync(order.Items, cancellationToken);

        var receiptDto = new ReceiptDto
        {
            OrderNumber = order.OrderNumber,
            ShopName = shop.ReceiptConfig?.ReceiptShopName ?? shop.ShopName,
            ShopAddress = $"{shop.Address.Street}, {shop.Address.City}, {shop.Address.Country}",
            ShopPhone = shop.Contact?.Phone,
            ShopEmail = shop.Contact?.Email,
            LogoUrl = shop.LogoUrl,
            VatRegistrationNumber = shop.VatRegistrationNumber,
            PharmacyLicenseNumber = shop.PharmacyRegistrationNumber,

            HeaderText = shop.ReceiptConfig?.ReceiptHeaderText,
            FooterText = shop.ReceiptConfig?.ReceiptFooterText,
            ShowLogo = shop.ReceiptConfig?.ShowLogoOnReceipt ?? true,
            ShowQrCode = shop.ReceiptConfig?.ShowQrCode ?? false,
            ShowTaxBreakdown = shop.ReceiptConfig?.ShowTaxBreakdown ?? true,
            ShowVatNumber = shop.ReceiptConfig?.ShowVatNumber ?? true,
            ShowPharmacyLicense = shop.ReceiptConfig?.ShowPharmacyLicense ?? true,
            Language = language,
            PaperType = paperType,

            OrderDate = order.OrderDate,
            CustomerName = order.CustomerName,
            SalespersonName = cashierName,
            PaymentMethod = order.PaymentMethod?.ToString() ?? "Cash",

            Items = items,

            Subtotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            TaxRate = order.TaxAmount > 0 && order.SubTotal > 0
                ? (order.TaxAmount / order.SubTotal) * 100
                : 0,
            Total = order.TotalAmount,
            AmountPaid = order.AmountPaid,
            Change = order.ChangeGiven,
            Currency = shop.Currency,
        };

        var pdfBytes = await _pdfService.GenerateReceiptPdfAsync(receiptDto);
        var fileName = $"Receipt_{order.OrderNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

        return new GenerateReceiptPdfResult(pdfBytes, fileName);
    }

    private static PaperType ResolvePaperType(string? requested, string? shopConfigured)
    {
        if (!string.IsNullOrEmpty(requested)
            && Enum.TryParse<PaperType>(requested, ignoreCase: true, out var fromRequest))
        {
            return fromRequest;
        }

        if (!string.IsNullOrEmpty(shopConfigured)
            && Enum.TryParse<PaperType>(shopConfigured, ignoreCase: true, out var fromShop))
        {
            return fromShop;
        }

        return PaperType.A5;
    }

    private static string ResolveLanguage(string? requested, string? shopConfigured) =>
        !string.IsNullOrEmpty(requested) ? requested
        : !string.IsNullOrEmpty(shopConfigured) ? shopConfigured
        : "en-US";

    private async Task<string?> GetCashierNameAsync(
        string cashierId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(cashierId))
        {
            return null;
        }

        var shopUserName = await _context.ShopUsers
            .AsNoTracking()
            .Include(su => su.User)
            .Where(u => u.UserId == cashierId)
            .Select(u => u.User.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(shopUserName))
        {
            return shopUserName;
        }

        var userName = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == cashierId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        return userName ?? cashierId;
    }

    private async Task<List<ReceiptItemDto>> GetOrderItemsWithNamesAsync(
        List<SalesOrderItem> orderItems,
        CancellationToken cancellationToken)
    {
        var drugIds = orderItems.Select(i => i.DrugId).Distinct().ToList();

        var drugsByDrugId = await _context.Drugs
            .AsNoTracking()
            .Where(d => drugIds.Contains(d.DrugId))
            .Select(d => new
            {
                d.Id,
                d.DrugId,
                d.BrandName,
                d.GenericName,
                d.Regulatory.IsPrescriptionRequired,
            })
            .ToDictionaryAsync(d => d.DrugId, cancellationToken);

        if (drugsByDrugId.Count == 0)
        {
            drugsByDrugId = await _context.Drugs
                .AsNoTracking()
                .Where(d => drugIds.Contains(d.Id))
                .Select(d => new
                {
                    d.Id,
                    d.DrugId,
                    d.BrandName,
                    d.GenericName,
                    d.Regulatory.IsPrescriptionRequired,
                })
                .ToDictionaryAsync(d => d.Id, cancellationToken);
        }

        return orderItems.Select(item =>
        {
            var drugInfo = drugsByDrugId.GetValueOrDefault(item.DrugId);
            var itemName = drugInfo != null
                ? (!string.IsNullOrEmpty(drugInfo.BrandName) ? drugInfo.BrandName : drugInfo.GenericName)
                : "Unknown Item";

            return new ReceiptItemDto
            {
                ItemName = itemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Total = item.TotalPrice,
                RequiresPrescription = drugInfo?.IsPrescriptionRequired ?? false,
            };
        }).ToList();
    }
}
