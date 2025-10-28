using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Auth.Entities;
using pos_system_api.Core.Domain.Auth.Enums;
using pos_system_api.Infrastructure.Auth;

namespace pos_system_api.Core.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IShopRepository _shopRepository;
    private readonly PasswordHasher _passwordHasher;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IShopRepository shopRepository,
        PasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _shopRepository = shopRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if username or email already exists
        if (await _userRepository.ExistsAsync(request.Username, request.Email, cancellationToken))
        {
            throw new InvalidOperationException("Username or email already exists");
        }

        // Parse system role (default to User, only SuperAdmins can create other SuperAdmins)
        var systemRole = SystemRole.User;
        if (!string.IsNullOrWhiteSpace(request.Role) && 
            Enum.TryParse<SystemRole>(request.Role, true, out var parsedRole))
        {
            systemRole = parsedRole;
        }

        // Validate ShopId if provided (for backward compatibility)
        // Note: In the new system, shop assignment should be done via ShopUser management
        if (!string.IsNullOrWhiteSpace(request.ShopId))
        {
            // Verify shop exists
            var shop = await _shopRepository.GetByIdAsync(request.ShopId, cancellationToken);
            if (shop == null)
            {
                throw new ArgumentException($"Shop with ID {request.ShopId} not found");
            }
            // Shop assignment will be handled after user creation if ShopId is provided
        }

        // Hash password
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // Create user entity
        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            FullName = request.FullName,
            SystemRole = systemRole,
            Phone = request.Phone,
            IsActive = true,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System" // TODO: Use authenticated user when available
        };

        // Save to database
        var createdUser = await _userRepository.CreateAsync(user, cancellationToken);

        return MapToUserDto(createdUser);
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            SystemRole = user.SystemRole.ToString(),
            Shops = user.ShopMemberships?
                .Where(sm => sm.IsActive)
                .Select(sm => new UserShopDto
                {
                    ShopId = sm.ShopId,
                    ShopName = sm.Shop?.ShopName ?? "",
                    Role = sm.Role.ToString(),
                    Permissions = sm.Permissions.Select(p => p.ToString()).ToList(),
                    IsOwner = sm.IsOwner,
                    IsActive = sm.IsActive,
                    JoinedDate = sm.JoinedDate,
                    // Include complete shop details with all configurations
                    ShopDetails = sm.Shop != null ? MapToShopDetailsDto(sm.Shop) : null
                })
                .ToList() ?? new List<UserShopDto>(),
            IsActive = user.IsActive,
            IsEmailVerified = user.IsEmailVerified,
            LastLoginAt = user.LastLoginAt,
            Phone = user.Phone,
            ProfileImageUrl = user.ProfileImageUrl
        };
    }

    private static ShopDetailsDto MapToShopDetailsDto(Core.Domain.Shops.Entities.Shop shop)
    {
        return new ShopDetailsDto
        {
            Id = shop.Id,
            ShopName = shop.ShopName,
            LegalName = shop.LegalName,
            LicenseNumber = shop.LicenseNumber,
            VatRegistrationNumber = shop.VatRegistrationNumber,
            PharmacyRegistrationNumber = shop.PharmacyRegistrationNumber,
            Address = new AddressDetailsDto
            {
                Street = shop.Address.Street,
                City = shop.Address.City,
                State = shop.Address.State,
                ZipCode = shop.Address.ZipCode,
                Country = shop.Address.Country
            },
            Contact = new ContactDetailsDto
            {
                Phone = shop.Contact.Phone,
                Email = shop.Contact.Email,
                Website = shop.Contact.Website
            },
            LogoUrl = shop.LogoUrl,
            ShopImageUrls = shop.ShopImageUrls,
            BrandColorPrimary = shop.BrandColorPrimary,
            BrandColorSecondary = shop.BrandColorSecondary,
            ReceiptConfig = new ReceiptConfigDetailsDto
            {
                ReceiptShopName = shop.ReceiptConfig.ReceiptShopName,
                HeaderText = shop.ReceiptConfig.ReceiptHeaderText,
                FooterText = shop.ReceiptConfig.ReceiptFooterText,
                ReturnPolicyText = shop.ReceiptConfig.ReturnPolicyText,
                PharmacistName = shop.ReceiptConfig.PharmacistName,
                ShowLogoOnReceipt = shop.ReceiptConfig.ShowLogoOnReceipt,
                ShowTaxBreakdown = shop.ReceiptConfig.ShowTaxBreakdown,
                ShowBarcode = shop.ReceiptConfig.ShowBarcode,
                ShowQrCode = shop.ReceiptConfig.ShowQrCode,
                ReceiptWidth = shop.ReceiptConfig.ReceiptWidth,
                ReceiptLanguage = shop.ReceiptConfig.ReceiptLanguage,
                PharmacyWarningText = shop.ReceiptConfig.PharmacyWarningText
            },
            HardwareConfig = new HardwareConfigDetailsDto
            {
                ReceiptPrinterName = shop.HardwareConfig.ReceiptPrinterName,
                ReceiptPrinterConnectionType = shop.HardwareConfig.ReceiptPrinterConnectionType,
                ReceiptPrinterIpAddress = shop.HardwareConfig.ReceiptPrinterIpAddress,
                ReceiptPrinterPort = shop.HardwareConfig.ReceiptPrinterPort,
                BarcodePrinterName = shop.HardwareConfig.BarcodePrinterName,
                BarcodePrinterConnectionType = shop.HardwareConfig.BarcodePrinterConnectionType,
                BarcodePrinterIpAddress = shop.HardwareConfig.BarcodePrinterIpAddress,
                BarcodeLabelSize = shop.HardwareConfig.BarcodeLabelSize.ToString(),
                BarcodeScannerModel = shop.HardwareConfig.BarcodeScannerModel,
                BarcodeScannerConnectionType = shop.HardwareConfig.BarcodeScannerConnectionType,
                AutoSubmitOnScan = shop.HardwareConfig.AutoSubmitOnScan,
                CashDrawerModel = shop.HardwareConfig.CashDrawerModel,
                CashDrawerEnabled = shop.HardwareConfig.CashDrawerEnabled,
                CashDrawerOpenCommand = shop.HardwareConfig.CashDrawerOpenCommand,
                PaymentTerminalModel = shop.HardwareConfig.PaymentTerminalModel,
                PaymentTerminalConnectionType = shop.HardwareConfig.PaymentTerminalConnectionType,
                PaymentTerminalIpAddress = shop.HardwareConfig.PaymentTerminalIpAddress,
                IntegratedPayments = shop.HardwareConfig.IntegratedPayments,
                PosTerminalId = shop.HardwareConfig.PosTerminalId,
                PosTerminalName = shop.HardwareConfig.PosTerminalName,
                CustomerDisplayEnabled = shop.HardwareConfig.CustomerDisplayEnabled,
                CustomerDisplayType = shop.HardwareConfig.CustomerDisplayType
            },
            Currency = shop.Currency,
            DefaultTaxRate = shop.DefaultTaxRate,
            AutoReorderEnabled = shop.AutoReorderEnabled,
            LowStockAlertThreshold = shop.LowStockAlertThreshold,
            OperatingHours = shop.OperatingHours,
            RequiresPrescriptionVerification = shop.RequiresPrescriptionVerification,
            AllowsControlledSubstances = shop.AllowsControlledSubstances,
            AcceptedInsuranceProviders = shop.AcceptedInsuranceProviders,
            Status = shop.Status.ToString(),
            RegistrationDate = shop.RegistrationDate,
            CreatedAt = shop.CreatedAt,
            LastUpdated = shop.LastUpdated
        };
    }
}
