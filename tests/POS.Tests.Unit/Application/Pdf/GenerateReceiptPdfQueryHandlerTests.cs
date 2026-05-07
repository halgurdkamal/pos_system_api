using Microsoft.Extensions.Logging.Abstractions;
using POS.Tests.Unit.Infrastructure;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Pdf.DTOs;
using pos_system_api.Core.Application.Pdf.Queries.GenerateReceiptPdf;
using pos_system_api.Core.Domain.Common.ValueObjects;
using pos_system_api.Core.Domain.Sales.Entities;
using pos_system_api.Core.Domain.Shops.Entities;
using pos_system_api.Infrastructure.Data;

namespace POS.Tests.Unit.Application.Pdf;

public class GenerateReceiptPdfQueryHandlerTests
{
    private static readonly byte[] StubPdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"

    private static ApplicationDbContext NewDbContext() => TestDbContextFactory.Create();

    private static GenerateReceiptPdfQueryHandler NewHandler(ApplicationDbContext db, IPdfService? pdf = null) =>
        new(db, pdf ?? new StubPdfService(), NullLogger<GenerateReceiptPdfQueryHandler>.Instance);

    [Fact]
    public async Task Handle_WhenOrderDoesNotExist_ReturnsNull()
    {
        using var db = NewDbContext();

        var handler = NewHandler(db);

        var result = await handler.Handle(
            new GenerateReceiptPdfQuery("SO-NOPE", null, null),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenOrderShopDoesNotExist_ReturnsNull()
    {
        using var db = NewDbContext();
        var order = SeedOrder(db, shopId: "SHOP-MISSING");
        await db.SaveChangesAsync();

        var handler = NewHandler(db);

        var result = await handler.Handle(
            new GenerateReceiptPdfQuery(order.OrderNumber, null, null),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsPdfBytesAndFileName()
    {
        using var db = NewDbContext();
        var shop = SeedShop(db);
        var order = SeedOrder(db, shopId: shop.Id);
        await db.SaveChangesAsync();

        var pdf = new StubPdfService();
        var handler = NewHandler(db, pdf);

        var result = await handler.Handle(
            new GenerateReceiptPdfQuery(order.OrderNumber, null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.PdfBytes.Should().BeEquivalentTo(StubPdfBytes);
        result.FileName.Should().StartWith($"Receipt_{order.OrderNumber}_").And.EndWith(".pdf");
        pdf.LastReceipt.Should().NotBeNull();
        pdf.LastReceipt!.OrderNumber.Should().Be(order.OrderNumber);
    }

    [Fact]
    public async Task Handle_RequestPaperType_OverridesShopConfig()
    {
        using var db = NewDbContext();
        var shop = SeedShop(db, paperType: "A4");
        var order = SeedOrder(db, shopId: shop.Id);
        await db.SaveChangesAsync();

        var pdf = new StubPdfService();
        var handler = NewHandler(db, pdf);

        var result = await handler.Handle(
            new GenerateReceiptPdfQuery(order.OrderNumber, null, "Thermal80mm"),
            CancellationToken.None);

        result.Should().NotBeNull();
        pdf.LastReceipt!.PaperType.Should().Be(PaperType.Thermal80mm);
    }

    [Fact]
    public async Task Handle_PaperTypeFallsBackToShopConfig_WhenRequestEmpty()
    {
        using var db = NewDbContext();
        var shop = SeedShop(db, paperType: "A4");
        var order = SeedOrder(db, shopId: shop.Id);
        await db.SaveChangesAsync();

        var pdf = new StubPdfService();
        var handler = NewHandler(db, pdf);

        var result = await handler.Handle(
            new GenerateReceiptPdfQuery(order.OrderNumber, null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        pdf.LastReceipt!.PaperType.Should().Be(PaperType.A4);
    }

    [Fact]
    public async Task Handle_PaperTypeFallsBackToA5_WhenRequestUnknownAndShopUnknown()
    {
        using var db = NewDbContext();
        var shop = SeedShop(db, paperType: "Bogus");
        var order = SeedOrder(db, shopId: shop.Id);
        await db.SaveChangesAsync();

        var pdf = new StubPdfService();
        var handler = NewHandler(db, pdf);

        var result = await handler.Handle(
            new GenerateReceiptPdfQuery(order.OrderNumber, null, "AlsoBogus"),
            CancellationToken.None);

        result.Should().NotBeNull();
        pdf.LastReceipt!.PaperType.Should().Be(PaperType.A5);
    }

    [Fact]
    public async Task Handle_LanguageRequest_OverridesShopConfig()
    {
        using var db = NewDbContext();
        var shop = SeedShop(db, language: "en-US");
        var order = SeedOrder(db, shopId: shop.Id);
        await db.SaveChangesAsync();

        var pdf = new StubPdfService();
        var handler = NewHandler(db, pdf);

        var result = await handler.Handle(
            new GenerateReceiptPdfQuery(order.OrderNumber, "ar", null),
            CancellationToken.None);

        result.Should().NotBeNull();
        pdf.LastReceipt!.Language.Should().Be("ar");
    }

    [Fact]
    public async Task Handle_TaxRate_IsZeroWhenSubTotalIsZero()
    {
        using var db = NewDbContext();
        var shop = SeedShop(db);
        var order = SeedOrder(db, shopId: shop.Id);
        // No items, SubTotal = 0
        await db.SaveChangesAsync();

        var pdf = new StubPdfService();
        var handler = NewHandler(db, pdf);

        var result = await handler.Handle(
            new GenerateReceiptPdfQuery(order.OrderNumber, null, null),
            CancellationToken.None);

        result.Should().NotBeNull();
        pdf.LastReceipt!.TaxRate.Should().Be(0, "no division-by-zero on empty orders");
    }

    // ----- helpers -----

    private static SalesOrder SeedOrder(ApplicationDbContext db, string shopId)
    {
        var order = new SalesOrder(
            shopId: shopId,
            cashierId: "CASHIER-1",
            customerName: "Test Customer");
        db.SalesOrders.Add(order);
        return order;
    }

    private static Shop SeedShop(
        ApplicationDbContext db,
        string? paperType = null,
        string? language = null)
    {
        var shop = new Shop(
            shopName: "Test Pharmacy",
            legalName: "Test Pharmacy LLC",
            licenseNumber: "LIC-1",
            address: new Address("1 Test St", "Testville", "TS", "00000", "Testland"),
            contact: new Contact());

        if (paperType != null)
        {
            shop.ReceiptConfig.PaperType = paperType;
        }
        if (language != null)
        {
            shop.ReceiptConfig.ReceiptLanguage = language;
        }

        shop.Currency = "USD";
        db.Shops.Add(shop);
        return shop;
    }

    private sealed class StubPdfService : IPdfService
    {
        public ReceiptDto? LastReceipt { get; private set; }

        public Task<byte[]> GenerateReceiptPdfAsync(ReceiptDto receiptData)
        {
            LastReceipt = receiptData;
            return Task.FromResult(StubPdfBytes);
        }

        public Task GenerateReceiptPdfToFileAsync(ReceiptDto receiptData, string filePath) =>
            Task.CompletedTask;
    }
}
