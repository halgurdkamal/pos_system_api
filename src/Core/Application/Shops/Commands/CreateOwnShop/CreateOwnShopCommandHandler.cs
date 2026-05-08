using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Shops.DTOs;
using pos_system_api.Core.Domain.Common.ValueObjects;
using pos_system_api.Core.Domain.Shops.Entities;
using pos_system_api.Core.Domain.Auth.Enums;

namespace pos_system_api.Core.Application.Shops.Commands.CreateOwnShop;

/// <summary>
/// Handler for CreateOwnShopCommand
/// Allows regular users to create their own shop and become the owner
/// </summary>
public class CreateOwnShopCommandHandler : IRequestHandler<CreateOwnShopCommand, ShopDto>
{
    private readonly IShopRepository _shopRepository;
    private readonly IShopUserRepository _shopUserRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOwnShopCommandHandler(
        IShopRepository shopRepository,
        IShopUserRepository shopUserRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _shopRepository = shopRepository;
        _shopUserRepository = shopUserRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShopDto> Handle(CreateOwnShopCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify user exists
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                throw new InvalidOperationException("User account is inactive");
            }

            // Validate license number uniqueness if provided
            if (!string.IsNullOrEmpty(request.LicenseNumber))
            {
                if (await _shopRepository.LicenseNumberExistsAsync(request.LicenseNumber, cancellationToken: cancellationToken))
                {
                    throw new InvalidOperationException($"Shop with license number '{request.LicenseNumber}' already exists");
                }
            }

            // Create Address value object
            var address = new Address(
                request.Address ?? string.Empty,
                request.City ?? string.Empty,
                request.State ?? string.Empty,
                request.PostalCode ?? string.Empty,
                request.Country ?? "Iraq" // Default country
            );

            // Create Contact value object
            var contact = new Contact(
                request.PhoneNumber ?? string.Empty,
                request.Email ?? string.Empty,
                null // Website (optional, can be added later)
            );

            // Create Shop entity
            var shop = new Shop(
                request.ShopName,
                request.ShopName, // Use same name as legal name for user-created shops
                request.LicenseNumber ?? $"TEMP-{Guid.NewGuid().ToString().Substring(0, 8)}", // Generate temp license if not provided
                address,
                contact
            );

            // Set additional properties
            if (!string.IsNullOrEmpty(request.TaxId))
            {
                shop.VatRegistrationNumber = request.TaxId;
            }

            if (!string.IsNullOrEmpty(request.Description))
            {
                // Truncate description to 100 characters if needed (database constraint)
                shop.PharmacyRegistrationNumber = request.Description.Length > 100
                    ? request.Description.Substring(0, 100)
                    : request.Description;
            }

            // Save shop
            var createdShop = await _shopRepository.AddAsync(shop, cancellationToken);

            // Create ShopUser membership with Owner role and the canonical
            // Owner permission set. SetRole assigns both Role and Permissions
            // from ShopRolePermissions.GetPermissionsForRole.
            var shopUser = new ShopUser();
            shopUser.ShopId = createdShop.Id;
            shopUser.UserId = request.UserId;
            shopUser.SetRole(ShopRole.Owner);
            shopUser.IsOwner = true;
            shopUser.JoinedDate = DateTime.UtcNow;
            shopUser.IsActive = true;
            shopUser.CreatedAt = DateTime.UtcNow;

            // Save shop user membership
            await _shopUserRepository.CreateAsync(shopUser, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Map to DTO
            return MapToDto(createdShop);
        }
        catch (Exception ex)
        {
            // Log the exception (you should inject ILogger in the constructor)
            throw new InvalidOperationException($"Failed to create shop: {ex.Message}", ex);
        }
    }

    private static ShopDto MapToDto(Shop shop)
    {
        return new ShopDto
        {
            Id = shop.Id,
            ShopName = shop.ShopName,
            LegalName = shop.LegalName,
            LicenseNumber = shop.LicenseNumber,
            Address = new Common.DTOs.AddressDto
            {
                Street = shop.Address?.Street ?? string.Empty,
                City = shop.Address?.City ?? string.Empty,
                State = shop.Address?.State ?? string.Empty,
                ZipCode = shop.Address?.ZipCode ?? string.Empty,
                Country = shop.Address?.Country ?? string.Empty
            },
            Contact = new Common.DTOs.ContactDto
            {
                Phone = shop.Contact?.Phone ?? string.Empty,
                Email = shop.Contact?.Email ?? string.Empty,
                Website = shop.Contact?.Website ?? string.Empty
            },
            VatRegistrationNumber = shop.VatRegistrationNumber,
            PharmacyRegistrationNumber = shop.PharmacyRegistrationNumber,
            Status = shop.Status.ToString(),
            RegistrationDate = shop.RegistrationDate,
            CreatedAt = shop.CreatedAt,
            UpdatedAt = shop.LastUpdated ?? DateTime.UtcNow,
            LastModifiedDate = shop.LastUpdated
        };
    }
}
