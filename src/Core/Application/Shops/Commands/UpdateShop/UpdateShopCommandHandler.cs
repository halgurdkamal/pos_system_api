using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Shops.DTOs;
using pos_system_api.Core.Domain.Common.ValueObjects;

namespace pos_system_api.Core.Application.Shops.Commands.UpdateShop;

/// <summary>
/// Handler for UpdateShopCommand
/// </summary>
public class UpdateShopCommandHandler : IRequestHandler<UpdateShopCommand, ShopDto>
{
    private readonly IShopRepository _shopRepository;

    public UpdateShopCommandHandler(IShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
    }

    public async Task<ShopDto> Handle(UpdateShopCommand request, CancellationToken cancellationToken)
    {
        var dto = request.ShopData;

        // Get existing shop
        var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
        if (shop == null)
        {
            throw new InvalidOperationException($"Shop with ID '{request.ShopId}' not found.");
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

        // Update shop info using domain method
        shop.UpdateShopInfo(dto.ShopName, dto.LegalName, address, contact);

        // Update additional properties
        shop.VatRegistrationNumber = dto.VatRegistrationNumber;
        shop.PharmacyRegistrationNumber = dto.PharmacyRegistrationNumber;

        // Update branding
        shop.LogoUrl = dto.LogoUrl;
        shop.BrandColorPrimary = dto.BrandColorPrimary;
        shop.BrandColorSecondary = dto.BrandColorSecondary;

        // Update collections
        if (dto.ShopImageUrls != null)
            shop.ShopImageUrls = dto.ShopImageUrls;
        if (dto.OperatingHours != null)
            shop.OperatingHours = dto.OperatingHours;
        if (dto.AcceptedInsuranceProviders != null)
            shop.AcceptedInsuranceProviders = dto.AcceptedInsuranceProviders;

        // Update compliance settings
        shop.RequiresPrescriptionVerification = dto.RequiresPrescriptionVerification;
        shop.AllowsControlledSubstances = dto.AllowsControlledSubstances;

        // Save to database
        var updatedShop = await _shopRepository.UpdateAsync(shop, cancellationToken);

        // Map to DTO
        return ShopMapper.MapToDto(updatedShop);
    }
}
