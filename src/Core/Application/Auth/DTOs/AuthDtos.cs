namespace pos_system_api.Core.Application.Auth.DTOs;

public record TokenResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
}

public record UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string SystemRole { get; init; } = string.Empty; // "SuperAdmin" or "User"
    public List<UserShopDto> Shops { get; init; } = new(); // List of shops user has access to
    public bool IsActive { get; init; }
    public bool IsEmailVerified { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public string? Phone { get; init; }
    public string? ProfileImageUrl { get; init; }
}

public record UserShopDto
{
    public string ShopId { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty; // "Owner", "Manager", etc.
    public List<string> Permissions { get; init; } = new(); // List of permission names
    public bool IsOwner { get; init; }
    public bool IsActive { get; init; }
    public DateTime JoinedDate { get; init; }

    // Complete shop details with all configurations
    public ShopDetailsDto? ShopDetails { get; init; }
}

public record ShopDetailsDto
{
    public string Id { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public string LegalName { get; init; } = string.Empty;
    public string LicenseNumber { get; init; } = string.Empty;
    public string? VatRegistrationNumber { get; init; }
    public string? PharmacyRegistrationNumber { get; init; }

    // Contact and Address
    public AddressDetailsDto Address { get; init; } = new();
    public ContactDetailsDto Contact { get; init; } = new();

    // Branding
    public string? LogoUrl { get; init; }
    public List<string> ShopImageUrls { get; init; } = new();
    public string? BrandColorPrimary { get; init; }
    public string? BrandColorSecondary { get; init; }

    // Configuration - This is what you requested for custom shop/printer
    public ReceiptConfigDetailsDto ReceiptConfig { get; init; } = new();
    public HardwareConfigDetailsDto HardwareConfig { get; init; } = new();

    // Shop Settings
    public string Currency { get; init; } = "USD";
    public decimal DefaultTaxRate { get; init; }
    public bool AutoReorderEnabled { get; init; }
    public int LowStockAlertThreshold { get; init; }

    // Operating Schedule
    public Dictionary<string, string> OperatingHours { get; init; } = new();

    // Compliance
    public bool RequiresPrescriptionVerification { get; init; }
    public bool AllowsControlledSubstances { get; init; }
    public List<string> AcceptedInsuranceProviders { get; init; } = new();

    // Status
    public string Status { get; init; } = "Active";
    public DateTime RegistrationDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastUpdated { get; init; }
}

public record AddressDetailsDto
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}

public record ContactDetailsDto
{
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Website { get; init; }
}

public record ReceiptConfigDetailsDto
{
    public string ReceiptShopName { get; init; } = string.Empty;
    public string? HeaderText { get; init; }
    public string? FooterText { get; init; }
    public string? ReturnPolicyText { get; init; }
    public string? PharmacistName { get; init; }
    public bool ShowLogoOnReceipt { get; init; } = true;
    public bool ShowTaxBreakdown { get; init; } = true;
    public bool ShowBarcode { get; init; } = true;
    public bool ShowQrCode { get; init; } = false;
    public int ReceiptWidth { get; init; } = 80;
    public string ReceiptLanguage { get; init; } = "en";
    public string? PharmacyWarningText { get; init; }
}

public record HardwareConfigDetailsDto
{
    // Receipt Printer - This is what you specifically requested
    public string? ReceiptPrinterName { get; init; }
    public string? ReceiptPrinterConnectionType { get; init; }
    public string? ReceiptPrinterIpAddress { get; init; }
    public int? ReceiptPrinterPort { get; init; }

    // Barcode Label Printer
    public string? BarcodePrinterName { get; init; }
    public string? BarcodePrinterConnectionType { get; init; }
    public string? BarcodePrinterIpAddress { get; init; }
    public string BarcodeLabelSize { get; init; } = "Small";

    // Barcode Scanner
    public string? BarcodeScannerModel { get; init; }
    public string? BarcodeScannerConnectionType { get; init; }
    public bool AutoSubmitOnScan { get; init; } = true;

    // Cash Drawer
    public string? CashDrawerModel { get; init; }
    public bool CashDrawerEnabled { get; init; } = true;
    public string? CashDrawerOpenCommand { get; init; }

    // Payment Terminal
    public string? PaymentTerminalModel { get; init; }
    public string? PaymentTerminalConnectionType { get; init; }
    public string? PaymentTerminalIpAddress { get; init; }
    public bool IntegratedPayments { get; init; } = false;

    // POS Terminal Info
    public string? PosTerminalId { get; init; }
    public string? PosTerminalName { get; init; }

    // Customer Display
    public bool CustomerDisplayEnabled { get; init; } = false;
    public string? CustomerDisplayType { get; init; }
}

public record LoginRequestDto
{
    /// <summary>
    /// Username, email, or phone number to login with
    /// </summary>
    public string Identifier { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RegisterRequestDto
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? ShopId { get; init; }
    public string Role { get; init; } = "Staff"; // Default role
    public string? Phone { get; init; }
}

public record RefreshTokenRequestDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
