using pos_system_api.Core.Domain.Common.ValueObjects;
using pos_system_api.Core.Domain.Shops.Entities;
using pos_system_api.Core.Domain.Shops.ValueObjects;

namespace pos_system_api.Infrastructure.Data.Seeders;

/// <summary>
/// Seeder for creating sample pharmacy shops
/// </summary>
public static class ShopSeeder
{
    public static List<Shop> GetSeedData()
    {
        var shops = new List<Shop>();

        // Shop 1: City Center Pharmacy
        var shop1 = new Shop(
            shopName: "City Center Pharmacy",
            legalName: "City Center Pharmaceutical Services LLC",
            licenseNumber: "PHARM-2024-001",
            address: new Address
            {
                Street = "123 Main Street",
                City = "New York",
                State = "NY",
                ZipCode = "10001",
                Country = "USA"
            },
            contact: new Contact
            {
                Phone = "+1-212-555-0101",
                Email = "info@citycenterpharmacy.com",
                Website = "https://citycenterpharmacy.com"
            }
        )
        {
            VatRegistrationNumber = "VAT-NY-123456",
            PharmacyRegistrationNumber = "PHREG-NY-001",
            LogoUrl = "https://example.com/logos/citycenter.png",
            BrandColorPrimary = "#0066CC",
            BrandColorSecondary = "#00AAFF",
            RequiresPrescriptionVerification = true,
            AllowsControlledSubstances = true,
            AcceptedInsuranceProviders = new List<string> { "BlueCross", "Aetna", "UnitedHealthcare" },
            OperatingHours = new Dictionary<string, string>
            {
                { "Monday", "08:00-20:00" },
                { "Tuesday", "08:00-20:00" },
                { "Wednesday", "08:00-20:00" },
                { "Thursday", "08:00-20:00" },
                { "Friday", "08:00-20:00" },
                { "Saturday", "09:00-18:00" },
                { "Sunday", "10:00-16:00" }
            },
            Status = ShopStatus.Active,
            ReceiptConfig = new ReceiptConfiguration
            {
                ReceiptShopName = "City Center Pharmacy",
                ReceiptHeaderText = "Thank you for choosing City Center Pharmacy",
                ReceiptFooterText = "Visit us online at citycenterpharmacy.com",
                ReturnPolicyText = "Returns accepted within 30 days with receipt",
                PharmacistName = "Dr. Sarah Johnson, PharmD",
                ShowLogoOnReceipt = true,
                ShowTaxBreakdown = true,
                ShowBarcode = true,
                ShowQrCode = true,
                ReceiptWidth = 80,
                ReceiptLanguage = "en-US",
                PharmacyWarningText = "Keep out of reach of children. Follow dosage instructions."
            },
            HardwareConfig = new HardwareConfiguration
            {
                ReceiptPrinterName = "Epson TM-T88VI",
                ReceiptPrinterConnectionType = "USB",
                BarcodeScannerModel = "Honeywell Voyager 1400g",
                BarcodeScannerConnectionType = "USB",
                CashDrawerEnabled = true,
                CashDrawerModel = "APG Vasario 1616",
                PaymentTerminalModel = "Ingenico iCT250",
                PaymentTerminalConnectionType = "Network",
                IntegratedPayments = false,
                CustomerDisplayEnabled = true,
                PosTerminalId = "POS-CITYCENTER-01",
                PosTerminalName = "Front Counter 1"
            }
        };

        // Shop 2: Suburban Health Pharmacy
        var shop2 = new Shop(
            shopName: "Suburban Health Pharmacy",
            legalName: "Suburban Health Services Inc.",
            licenseNumber: "PHARM-2024-002",
            address: new Address
            {
                Street = "456 Oak Avenue",
                City = "Los Angeles",
                State = "CA",
                ZipCode = "90001",
                Country = "USA"
            },
            contact: new Contact
            {
                Phone = "+1-323-555-0202",
                Email = "contact@suburbanhealthpharmacy.com",
                Website = "https://suburbanhealthpharmacy.com"
            }
        )
        {
            VatRegistrationNumber = "VAT-CA-789012",
            PharmacyRegistrationNumber = "PHREG-CA-002",
            LogoUrl = "https://example.com/logos/suburban.png",
            BrandColorPrimary = "#00AA44",
            BrandColorSecondary = "#88DD88",
            RequiresPrescriptionVerification = true,
            AllowsControlledSubstances = false,
            AcceptedInsuranceProviders = new List<string> { "Kaiser", "Medicare", "Medicaid" },
            OperatingHours = new Dictionary<string, string>
            {
                { "Monday", "09:00-19:00" },
                { "Tuesday", "09:00-19:00" },
                { "Wednesday", "09:00-19:00" },
                { "Thursday", "09:00-19:00" },
                { "Friday", "09:00-19:00" },
                { "Saturday", "10:00-17:00" },
                { "Sunday", "Closed" }
            },
            Status = ShopStatus.Active
        };

        // Shop 3: Downtown Express Pharmacy (Pending)
        var shop3 = new Shop(
            shopName: "Downtown Express Pharmacy",
            legalName: "Downtown Express Pharmaceutical LLC",
            licenseNumber: "PHARM-2024-003",
            address: new Address
            {
                Street = "789 Broadway",
                City = "Chicago",
                State = "IL",
                ZipCode = "60601",
                Country = "USA"
            },
            contact: new Contact
            {
                Phone = "+1-312-555-0303",
                Email = "info@downtownexpress.com",
                Website = "https://downtownexpress.com"
            }
        )
        {
            VatRegistrationNumber = "VAT-IL-345678",
            PharmacyRegistrationNumber = "PHREG-IL-003",
            RequiresPrescriptionVerification = true,
            AllowsControlledSubstances = true,
            Status = ShopStatus.Suspended // Using Suspended instead of Pending
        };

        shops.Add(shop1);
        shops.Add(shop2);
        shops.Add(shop3);

        return shops;
    }
}
