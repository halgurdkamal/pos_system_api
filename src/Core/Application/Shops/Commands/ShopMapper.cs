using pos_system_api.Core.Application.Common.DTOs;
using pos_system_api.Core.Application.Shops.DTOs;
using pos_system_api.Core.Domain.Shops.Entities;
using pos_system_api.Core.Domain.Shops.ValueObjects;

namespace pos_system_api.Core.Application.Shops.Commands;

/// <summary>
/// Helper class for mapping Shop entity to ShopDto
/// </summary>
public static class ShopMapper
{
    public static ShopDto MapToDto(Shop shop)
    {
        return new ShopDto
        {
            Id = shop.Id,
            ShopName = shop.ShopName,
            LegalName = shop.LegalName,
            LicenseNumber = shop.LicenseNumber,
            VatRegistrationNumber = shop.VatRegistrationNumber ?? string.Empty,
            PharmacyRegistrationNumber = shop.PharmacyRegistrationNumber ?? string.Empty,
            Address = new AddressDto
            {
                Street = shop.Address.Street,
                City = shop.Address.City,
                State = shop.Address.State,
                ZipCode = shop.Address.ZipCode,
                Country = shop.Address.Country
            },
            Contact = new ContactDto
            {
                Phone = shop.Contact.Phone,
                Email = shop.Contact.Email,
                Website = shop.Contact.Website
            },
            LogoUrl = shop.LogoUrl,
            ShopImageUrls = shop.ShopImageUrls,
            BrandColorPrimary = shop.BrandColorPrimary,
            BrandColorSecondary = shop.BrandColorSecondary,
            ReceiptConfig = new ReceiptConfigurationDto
            {
                ReceiptShopName = shop.ReceiptConfig.ReceiptShopName,
                HeaderText = shop.ReceiptConfig.ReceiptHeaderText ?? string.Empty,
                FooterText = shop.ReceiptConfig.ReceiptFooterText ?? string.Empty,
                ReturnPolicyText = shop.ReceiptConfig.ReturnPolicyText ?? string.Empty,
                PharmacistName = shop.ReceiptConfig.PharmacistName ?? string.Empty,
                ShowLogoOnReceipt = shop.ReceiptConfig.ShowLogoOnReceipt,
                ShowTaxBreakdown = shop.ReceiptConfig.ShowTaxBreakdown,
                ShowBarcode = shop.ReceiptConfig.ShowBarcode,
                ShowQrCode = shop.ReceiptConfig.ShowQrCode,
                ReceiptWidth = shop.ReceiptConfig.ReceiptWidth,
                ReceiptLanguage = shop.ReceiptConfig.ReceiptLanguage,
                PharmacyWarningText = shop.ReceiptConfig.PharmacyWarningText ?? string.Empty
            },
            HardwareConfig = new HardwareConfigurationDto
            {
                ReceiptPrinterName = shop.HardwareConfig.ReceiptPrinterName ?? string.Empty,
                ReceiptPrinterConnection = shop.HardwareConfig.ReceiptPrinterConnectionType ?? "USB",
                ReceiptPrinterIpAddress = shop.HardwareConfig.ReceiptPrinterIpAddress,
                ReceiptPrinterPort = shop.HardwareConfig.ReceiptPrinterPort,
                BarcodePrinterName = shop.HardwareConfig.BarcodePrinterName,
                BarcodePrinterConnection = shop.HardwareConfig.BarcodePrinterConnectionType ?? "USB",
                BarcodePrinterIpAddress = shop.HardwareConfig.BarcodePrinterIpAddress,
                BarcodeLabelSize = shop.HardwareConfig.BarcodeLabelSize.ToString(),
                BarcodeScannerModel = shop.HardwareConfig.BarcodeScannerModel,
                BarcodeScannerConnection = shop.HardwareConfig.BarcodeScannerConnectionType ?? "USB",
                CashDrawerModel = shop.HardwareConfig.CashDrawerModel,
                CashDrawerEnabled = shop.HardwareConfig.CashDrawerEnabled,
                CashDrawerOpenCommand = shop.HardwareConfig.CashDrawerOpenCommand ?? string.Empty,
                PaymentTerminalModel = shop.HardwareConfig.PaymentTerminalModel,
                PaymentTerminalConnection = shop.HardwareConfig.PaymentTerminalConnectionType ?? "Serial",
                PaymentTerminalIpAddress = shop.HardwareConfig.PaymentTerminalIpAddress,
                PosTerminalId = shop.HardwareConfig.PosTerminalId ?? string.Empty,
                PosTerminalName = shop.HardwareConfig.PosTerminalName ?? string.Empty,
                CustomerDisplayEnabled = shop.HardwareConfig.CustomerDisplayEnabled,
                CustomerDisplayType = shop.HardwareConfig.CustomerDisplayType ?? "LED"
            },
            OperatingHours = shop.OperatingHours,
            RequiresPrescriptionVerification = shop.RequiresPrescriptionVerification,
            AllowsControlledSubstances = shop.AllowsControlledSubstances,
            AcceptedInsuranceProviders = shop.AcceptedInsuranceProviders,
            Status = shop.Status.ToString(),
            RegistrationDate = shop.RegistrationDate,
            LastModifiedDate = shop.LastUpdated,
            CreatedAt = shop.CreatedAt,
            UpdatedAt = shop.LastUpdated ?? shop.CreatedAt
        };
    }
}
