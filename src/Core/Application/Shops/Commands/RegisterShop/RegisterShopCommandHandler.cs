using MediatR;
using pos_system_api.Core.Application.Common.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Shops.DTOs;
using pos_system_api.Core.Domain.Common.ValueObjects;
using pos_system_api.Core.Domain.Shops.Entities;
using pos_system_api.Core.Domain.Shops.ValueObjects;

namespace pos_system_api.Core.Application.Shops.Commands.RegisterShop;

/// <summary>
/// Handler for RegisterShopCommand
/// </summary>
public class RegisterShopCommandHandler : IRequestHandler<RegisterShopCommand, ShopDto>
{
    private readonly IShopRepository _shopRepository;

    public RegisterShopCommandHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<ShopDto> Handle(RegisterShopCommand request, CancellationToken cancellationToken)
    {
        var dto = request.ShopData;

        // Validate license number uniqueness
        if (await _shopRepository.LicenseNumberExistsAsync(dto.LicenseNumber, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"Shop with license number '{dto.LicenseNumber}' already exists.");
        }

        // Map Address DTO to Address value object
        var address = new Address(
            dto.Address.Street,
            dto.Address.City,
            dto.Address.State,
            dto.Address.ZipCode,
            dto.Address.Country
        );

        // Map Contact DTO to Contact value object
        var contact = new Contact(
            dto.Contact.Phone,
            dto.Contact.Email,
            dto.Contact.Website
        );

        // Create Shop entity using simple constructor
        var shop = new Shop(
            dto.ShopName,
            dto.LegalName,
            dto.LicenseNumber,
            address,
            contact
        );

        // Set additional properties
        shop.VatRegistrationNumber = dto.VatRegistrationNumber;
        shop.PharmacyRegistrationNumber = dto.PharmacyRegistrationNumber;

        // Map Receipt Configuration if provided
        if (dto.ReceiptConfig != null)
        {
            shop.ReceiptConfig = new ReceiptConfiguration
            {
                ReceiptShopName = dto.ReceiptConfig.ReceiptShopName,
                ReceiptHeaderText = dto.ReceiptConfig.HeaderText,
                ReceiptFooterText = dto.ReceiptConfig.FooterText,
                ReturnPolicyText = dto.ReceiptConfig.ReturnPolicyText,
                PharmacistName = dto.ReceiptConfig.PharmacistName,
                ShowLogoOnReceipt = dto.ReceiptConfig.ShowLogoOnReceipt,
                ShowTaxBreakdown = dto.ReceiptConfig.ShowTaxBreakdown,
                ShowBarcode = dto.ReceiptConfig.ShowBarcode,
                ShowQrCode = dto.ReceiptConfig.ShowQrCode,
                ReceiptWidth = dto.ReceiptConfig.ReceiptWidth,
                ReceiptLanguage = dto.ReceiptConfig.ReceiptLanguage,
                PharmacyWarningText = dto.ReceiptConfig.PharmacyWarningText
            };
        }

        // Map Hardware Configuration if provided
        if (dto.HardwareConfig != null)
        {
            shop.HardwareConfig = new HardwareConfiguration
            {
                ReceiptPrinterName = dto.HardwareConfig.ReceiptPrinterName,
                ReceiptPrinterConnectionType = dto.HardwareConfig.ReceiptPrinterConnection,
                ReceiptPrinterIpAddress = dto.HardwareConfig.ReceiptPrinterIpAddress,
                ReceiptPrinterPort = dto.HardwareConfig.ReceiptPrinterPort,
                BarcodePrinterName = dto.HardwareConfig.BarcodePrinterName,
                BarcodePrinterConnectionType = dto.HardwareConfig.BarcodePrinterConnection,
                BarcodePrinterIpAddress = dto.HardwareConfig.BarcodePrinterIpAddress,
                BarcodeLabelSize = Enum.Parse<BarcodeLabelSize>(dto.HardwareConfig.BarcodeLabelSize),
                BarcodeScannerModel = dto.HardwareConfig.BarcodeScannerModel,
                BarcodeScannerConnectionType = dto.HardwareConfig.BarcodeScannerConnection,
                CashDrawerModel = dto.HardwareConfig.CashDrawerModel,
                CashDrawerEnabled = dto.HardwareConfig.CashDrawerEnabled,
                CashDrawerOpenCommand = dto.HardwareConfig.CashDrawerOpenCommand,
                PaymentTerminalModel = dto.HardwareConfig.PaymentTerminalModel,
                PaymentTerminalConnectionType = dto.HardwareConfig.PaymentTerminalConnection,
                PaymentTerminalIpAddress = dto.HardwareConfig.PaymentTerminalIpAddress,
                PosTerminalId = dto.HardwareConfig.PosTerminalId,
                PosTerminalName = dto.HardwareConfig.PosTerminalName,
                CustomerDisplayEnabled = dto.HardwareConfig.CustomerDisplayEnabled,
                CustomerDisplayType = dto.HardwareConfig.CustomerDisplayType
            };
        }

        // Set operating hours and other collections
        if (dto.OperatingHours != null)
            shop.OperatingHours = dto.OperatingHours;
        if (dto.ShopImageUrls != null)
            shop.ShopImageUrls = dto.ShopImageUrls;
        if (dto.AcceptedInsuranceProviders != null)
            shop.AcceptedInsuranceProviders = dto.AcceptedInsuranceProviders;

        // Set optional branding
        if (!string.IsNullOrEmpty(dto.LogoUrl))
            shop.LogoUrl = dto.LogoUrl;
        if (!string.IsNullOrEmpty(dto.BrandColorPrimary))
            shop.BrandColorPrimary = dto.BrandColorPrimary;
        if (!string.IsNullOrEmpty(dto.BrandColorSecondary))
            shop.BrandColorSecondary = dto.BrandColorSecondary;

        // Set compliance settings
        shop.RequiresPrescriptionVerification = dto.RequiresPrescriptionVerification;
        shop.AllowsControlledSubstances = dto.AllowsControlledSubstances;

        // Save to database
        var createdShop = await _shopRepository.AddAsync(shop, cancellationToken);

        // Map to DTO using shared mapper
        return ShopMapper.MapToDto(createdShop);
    }
}
