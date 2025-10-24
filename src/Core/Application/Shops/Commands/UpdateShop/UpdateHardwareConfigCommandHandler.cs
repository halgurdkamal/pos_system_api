using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Shops.DTOs;
using pos_system_api.Core.Domain.Shops.ValueObjects;

namespace pos_system_api.Core.Application.Shops.Commands.UpdateShop;

/// <summary>
/// Handler for UpdateHardwareConfigCommand
/// </summary>
public class UpdateHardwareConfigCommandHandler : IRequestHandler<UpdateHardwareConfigCommand, ShopDto>
{
    private readonly IShopRepository _shopRepository;

    public UpdateHardwareConfigCommandHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<ShopDto> Handle(UpdateHardwareConfigCommand request, CancellationToken cancellationToken)
    {
        var dto = request.HardwareConfig;

        // Get existing shop
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
        if (shop == null)
        {
            throw new InvalidOperationException($"Shop with ID '{request.ShopId}' not found.");
        }

        // Create new hardware configuration
        var hardwareConfig = new HardwareConfiguration
        {
            ReceiptPrinterName = dto.ReceiptPrinterName,
            ReceiptPrinterConnectionType = dto.ReceiptPrinterConnection,
            ReceiptPrinterIpAddress = dto.ReceiptPrinterIpAddress,
            ReceiptPrinterPort = dto.ReceiptPrinterPort,
            BarcodePrinterName = dto.BarcodePrinterName,
            BarcodePrinterConnectionType = dto.BarcodePrinterConnection,
            BarcodePrinterIpAddress = dto.BarcodePrinterIpAddress,
            BarcodeLabelSize = Enum.Parse<BarcodeLabelSize>(dto.BarcodeLabelSize),
            BarcodeScannerModel = dto.BarcodeScannerModel,
            BarcodeScannerConnectionType = dto.BarcodeScannerConnection,
            CashDrawerModel = dto.CashDrawerModel,
            CashDrawerEnabled = dto.CashDrawerEnabled,
            CashDrawerOpenCommand = dto.CashDrawerOpenCommand,
            PaymentTerminalModel = dto.PaymentTerminalModel,
            PaymentTerminalConnectionType = dto.PaymentTerminalConnection,
            PaymentTerminalIpAddress = dto.PaymentTerminalIpAddress,
            PosTerminalId = dto.PosTerminalId,
            PosTerminalName = dto.PosTerminalName,
            CustomerDisplayEnabled = dto.CustomerDisplayEnabled,
            CustomerDisplayType = dto.CustomerDisplayType
        };

        // Update using domain method
        shop.UpdateHardwareConfiguration(hardwareConfig);

        // Save to database
        var updatedShop = await _shopRepository.UpdateAsync(shop, cancellationToken);

        // Map to DTO
        return ShopMapper.MapToDto(updatedShop);
    }
}
