using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Shops.DTOs;
using pos_system_api.Core.Domain.Shops.ValueObjects;

namespace pos_system_api.Core.Application.Shops.Commands.UpdateShop;

/// <summary>
/// Handler for UpdateReceiptConfigCommand
/// </summary>
public class UpdateReceiptConfigCommandHandler : IRequestHandler<UpdateReceiptConfigCommand, ShopDto>
{
    private readonly IShopRepository _shopRepository;

    public UpdateReceiptConfigCommandHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<ShopDto> Handle(UpdateReceiptConfigCommand request, CancellationToken cancellationToken)
    {
        var dto = request.ReceiptConfig;

        // Get existing shop
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
        if (shop == null)
        {
            throw new InvalidOperationException($"Shop with ID '{request.ShopId}' not found.");
        }

        // Create new receipt configuration
        var receiptConfig = new ReceiptConfiguration
        {
            ReceiptShopName = dto.ReceiptShopName,
            ReceiptHeaderText = dto.HeaderText,
            ReceiptFooterText = dto.FooterText,
            ReturnPolicyText = dto.ReturnPolicyText,
            PharmacistName = dto.PharmacistName,
            ShowLogoOnReceipt = dto.ShowLogoOnReceipt,
            ShowTaxBreakdown = dto.ShowTaxBreakdown,
            ShowBarcode = dto.ShowBarcode,
            ShowQrCode = dto.ShowQrCode,
            ReceiptWidth = dto.ReceiptWidth,
            ReceiptLanguage = dto.ReceiptLanguage,
            PharmacyWarningText = dto.PharmacyWarningText
        };

        // Update using domain method
        shop.UpdateReceiptConfiguration(receiptConfig);

        // Save to database
        var updatedShop = await _shopRepository.UpdateAsync(shop, cancellationToken);

        // Map to DTO
        return ShopMapper.MapToDto(updatedShop);
    }
}
