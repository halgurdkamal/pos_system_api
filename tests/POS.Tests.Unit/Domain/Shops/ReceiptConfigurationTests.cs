using pos_system_api.Core.Domain.Shops.ValueObjects;

namespace POS.Tests.Unit.Domain.Shops;

public class ReceiptConfigurationTests
{
    [Fact]
    public void DefaultConstructor_AppliesSensibleDefaults()
    {
        var config = new ReceiptConfiguration();

        config.ShowLogoOnReceipt.Should().BeTrue();
        config.ShowTaxBreakdown.Should().BeTrue();
        config.ShowBarcode.Should().BeTrue();
        config.ShowPharmacyLicense.Should().BeTrue();
        config.ShowVatNumber.Should().BeTrue();
        config.ShowQrCode.Should().BeFalse();
        config.PrintDuplicateReceipt.Should().BeFalse();

        config.ReceiptWidth.Should().Be(80);
        config.ReceiptLanguage.Should().Be("en-US");
        config.PaperType.Should().Be("A5");
    }

    [Fact]
    public void ParameterizedConstructor_AssignsBrandingFields()
    {
        var config = new ReceiptConfiguration(
            receiptShopName: "Acme Pharmacy",
            headerText: "Welcome!",
            footerText: "Thank you for shopping");

        config.ReceiptShopName.Should().Be("Acme Pharmacy");
        config.ReceiptHeaderText.Should().Be("Welcome!");
        config.ReceiptFooterText.Should().Be("Thank you for shopping");

        config.PaperType.Should().Be("A5", "constructor must keep default paper type");
        config.ShowLogoOnReceipt.Should().BeTrue();
    }

    [Theory]
    [InlineData("A4")]
    [InlineData("A5")]
    [InlineData("Thermal80mm")]
    [InlineData("Thermal58mm")]
    public void PaperType_AcceptsKnownTypes(string paperType)
    {
        var config = new ReceiptConfiguration { PaperType = paperType };

        config.PaperType.Should().Be(paperType);
    }
}
