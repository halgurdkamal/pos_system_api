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

    public CreateOwnShopCommandHandler(
        IShopRepository shopRepository,
        IShopUserRepository shopUserRepository,
        IUserRepository userRepository)
    {
        _shopRepository = shopRepository;
        _shopUserRepository = shopUserRepository;
        _userRepository = userRepository;
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

            // Create ShopUser membership with Owner role
            var shopUser = new ShopUser();
            shopUser.ShopId = createdShop.Id;
            shopUser.UserId = request.UserId;
            shopUser.Role = ShopRole.Owner;
            shopUser.IsOwner = true;
            shopUser.JoinedDate = DateTime.UtcNow;
            shopUser.IsActive = true;
            shopUser.CreatedAt = DateTime.UtcNow;

            // Grant all essential permissions to owner
            shopUser.AddPermission(Permission.ProcessSales);
        shopUser.AddPermission(Permission.ViewSales);
        shopUser.AddPermission(Permission.RefundSales);
        shopUser.AddPermission(Permission.ApplyDiscounts);
        shopUser.AddPermission(Permission.ViewInventory);
        shopUser.AddPermission(Permission.AddStock);
        shopUser.AddPermission(Permission.ReduceStock);
        shopUser.AddPermission(Permission.UpdatePricing);
        shopUser.AddPermission(Permission.ManageProducts);
        shopUser.AddPermission(Permission.StockAudit);
        shopUser.AddPermission(Permission.ViewOrders);
        shopUser.AddPermission(Permission.CreateOrders);
        shopUser.AddPermission(Permission.ApproveOrders);
        shopUser.AddPermission(Permission.ReceiveOrders);
        shopUser.AddPermission(Permission.CancelOrders);
        shopUser.AddPermission(Permission.ViewSuppliers);
        shopUser.AddPermission(Permission.ManageSuppliers);
        shopUser.AddPermission(Permission.ViewCustomers);
        shopUser.AddPermission(Permission.ManageCustomers);
        shopUser.AddPermission(Permission.ViewStaff);
        shopUser.AddPermission(Permission.InviteStaff);
        shopUser.AddPermission(Permission.RemoveStaff);
        shopUser.AddPermission(Permission.UpdateStaffPermissions);
        shopUser.AddPermission(Permission.ViewReports);
        shopUser.AddPermission(Permission.ExportReports);
        shopUser.AddPermission(Permission.ViewAnalytics);
        shopUser.AddPermission(Permission.UpdateShopInfo);
        shopUser.AddPermission(Permission.UpdateReceiptConfig);
        shopUser.AddPermission(Permission.UpdateHardwareConfig);
        shopUser.AddPermission(Permission.ManagePaymentMethods);
        shopUser.AddPermission(Permission.ManageTaxes);
        shopUser.AddPermission(Permission.ViewFinancials);
        shopUser.AddPermission(Permission.RecordExpenses);
        shopUser.AddPermission(Permission.CloseCashRegister);
        shopUser.AddPermission(Permission.ViewAuditLogs);
        shopUser.AddPermission(Permission.BackupData);

        // Save shop user membership
        await _shopUserRepository.CreateAsync(shopUser, cancellationToken);

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
                Street = shop.Address.Street,
                City = shop.Address.City,
                State = shop.Address.State,
                ZipCode = shop.Address.ZipCode,
                Country = shop.Address.Country
            },
            Contact = new Common.DTOs.ContactDto
            {
                Phone = shop.Contact.Phone,
                Email = shop.Contact.Email,
                Website = shop.Contact.Website
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
