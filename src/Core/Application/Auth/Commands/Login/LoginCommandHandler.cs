using MediatR;
using pos_system_api.Core.Application.Auth.DTOs;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;

namespace pos_system_api.Core.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponseDto>
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(
        IUserRepository userRepository,
        PasswordHasher passwordHasher,
        JwtTokenService jwtTokenService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    public async Task<TokenResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Get user by identifier (username, email, or phone)
        var user = await _userRepository.GetByIdentifierAsync(request.Identifier, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Check if account is locked
        if (user.IsLocked())
        {
            throw new UnauthorizedAccessException($"Account is locked until {user.LockedUntil:yyyy-MM-dd HH:mm:ss}");
        }

        // Check if account is active
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            // Record failed login attempt
            user.RecordFailedLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);

            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Update last login
        user.UpdateLastLogin();

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Get refresh token expiry from config
        var refreshTokenExpiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(refreshTokenExpiryDays);

        // Update user with refresh token
        user.UpdateRefreshToken(refreshToken, refreshTokenExpiry);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Get access token expiry from config
        var accessTokenExpiryMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(accessTokenExpiryMinutes);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = accessTokenExpiry,
            User = MapToUserDto(user)
        };
    }

    private static UserDto MapToUserDto(Core.Domain.Auth.Entities.User user)
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
