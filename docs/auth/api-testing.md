# API Testing Guide - Complete Shop Details in Auth Response

## Test the Enhanced Authentication Endpoints

### Prerequisites

- API running on: `http://localhost:5000`
- Have a valid user account with shop access
- Use tools like Postman, cURL, or REST Client

---

## 1. Test Login Endpoint (Returns Complete Shop Details)

### Request

```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "identifier": "your_username_or_email",
  "password": "your_password"
}
```

### cURL Command

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "identifier": "your_username",
    "password": "your_password"
  }'
```

### PowerShell Command

```powershell
$body = @{
    identifier = "your_username"
    password = "your_password"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

### Expected Response Structure

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "expiresAt": "2025-10-25T16:33:00Z",
  "user": {
    "id": "USER-123ABC",
    "username": "john_doe",
    "email": "john@example.com",
    "fullName": "John Doe",
    "systemRole": "User",
    "isActive": true,
    "isEmailVerified": true,
    "lastLoginAt": "2025-10-25T15:33:00Z",
    "phone": "+1-555-0123",
    "shops": [
      {
        "shopId": "SHOP-ABC123",
        "shopName": "Main Pharmacy",
        "role": "Owner",
        "permissions": [
          "ViewDrugs",
          "CreateSalesOrder",
          "ViewInventory",
          "ConfigureHardware"
        ],
        "isOwner": true,
        "isActive": true,
        "joinedDate": "2025-01-15T10:00:00Z",
        "shopDetails": {
          "id": "SHOP-ABC123",
          "shopName": "Main Pharmacy",
          "legalName": "Main Pharmacy LLC",
          "licenseNumber": "PH-2025-001",
          "vatRegistrationNumber": "VAT-123456",
          "pharmacyRegistrationNumber": "PHARM-001",
          "address": {
            "street": "123 Main Street",
            "city": "New York",
            "state": "NY",
            "zipCode": "10001",
            "country": "USA"
          },
          "contact": {
            "phone": "+1-555-0100",
            "email": "contact@mainpharmacy.com",
            "website": "https://mainpharmacy.com"
          },
          "logoUrl": "https://cdn.example.com/logos/pharmacy-logo.png",
          "shopImageUrls": ["https://cdn.example.com/shops/main-front.jpg"],
          "brandColorPrimary": "#007BFF",
          "brandColorSecondary": "#6C757D",
          "receiptConfig": {
            "receiptShopName": "Main Pharmacy",
            "headerText": "Welcome to Main Pharmacy",
            "footerText": "Thank you for your business!",
            "returnPolicyText": "Returns accepted within 30 days",
            "pharmacistName": "Dr. Jane Smith",
            "showLogoOnReceipt": true,
            "showTaxBreakdown": true,
            "showBarcode": true,
            "showQrCode": false,
            "receiptWidth": 80,
            "receiptLanguage": "en",
            "pharmacyWarningText": "Keep out of reach of children"
          },
          "hardwareConfig": {
            "receiptPrinterName": "EPSON TM-T88V",
            "receiptPrinterConnectionType": "Network",
            "receiptPrinterIpAddress": "192.168.1.100",
            "receiptPrinterPort": 9100,
            "barcodePrinterName": "Zebra ZD420",
            "barcodePrinterConnectionType": "USB",
            "barcodePrinterIpAddress": null,
            "barcodeLabelSize": "Small",
            "barcodeScannerModel": "Honeywell 1900",
            "barcodeScannerConnectionType": "USB",
            "autoSubmitOnScan": true,
            "cashDrawerModel": "APG Cash Drawer",
            "cashDrawerEnabled": true,
            "cashDrawerOpenCommand": "ESC/POS Open Command",
            "paymentTerminalModel": "Verifone VX520",
            "paymentTerminalConnectionType": "Network",
            "paymentTerminalIpAddress": "192.168.1.101",
            "integratedPayments": false,
            "posTerminalId": "POS-001",
            "posTerminalName": "Front Counter 1",
            "customerDisplayEnabled": false,
            "customerDisplayType": null
          },
          "currency": "USD",
          "defaultTaxRate": 8.5,
          "autoReorderEnabled": true,
          "lowStockAlertThreshold": 50,
          "operatingHours": {
            "Monday": "9:00-18:00",
            "Tuesday": "9:00-18:00",
            "Wednesday": "9:00-18:00",
            "Thursday": "9:00-18:00",
            "Friday": "9:00-18:00",
            "Saturday": "10:00-16:00",
            "Sunday": "Closed"
          },
          "requiresPrescriptionVerification": true,
          "allowsControlledSubstances": false,
          "acceptedInsuranceProviders": [
            "BlueCross",
            "Aetna",
            "UnitedHealthcare"
          ],
          "status": "Active",
          "registrationDate": "2025-01-15T10:00:00Z",
          "createdAt": "2025-01-15T10:00:00Z",
          "lastUpdated": "2025-10-20T14:30:00Z"
        }
      }
    ]
  }
}
```

---

## 2. Test Register Endpoint (Also Returns Complete Shop Details)

### Request

```http
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "newuser",
  "email": "newuser@example.com",
  "password": "SecurePassword123!",
  "fullName": "New User",
  "shopId": "SHOP-ABC123",
  "role": "Staff",
  "phone": "+1-555-0199"
}
```

### PowerShell Command

```powershell
$body = @{
    username = "newuser"
    email = "newuser@example.com"
    password = "SecurePassword123!"
    fullName = "New User"
    shopId = "SHOP-ABC123"
    role = "Staff"
    phone = "+1-555-0199"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/auth/register" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

---

## 3. Test Refresh Token Endpoint

### Request

```http
POST http://localhost:5000/api/auth/refresh
Content-Type: application/json

{
  "accessToken": "your_expired_access_token",
  "refreshToken": "your_refresh_token"
}
```

### PowerShell Command

```powershell
$body = @{
    accessToken = "your_access_token"
    refreshToken = "your_refresh_token"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/auth/refresh" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

---

## Key Points to Verify

### ✅ Printer Configuration Available

Check that `user.shops[0].shopDetails.hardwareConfig` contains:

- `receiptPrinterName`
- `receiptPrinterConnectionType` (USB, Network, Bluetooth)
- `receiptPrinterIpAddress` (for network printers)
- `receiptPrinterPort` (typically 9100 for ESC/POS)

### ✅ Receipt Configuration Available

Check that `user.shops[0].shopDetails.receiptConfig` contains:

- `receiptShopName`
- `headerText` and `footerText`
- `showLogoOnReceipt`
- `receiptWidth` (typically 80mm)

### ✅ Complete Shop Settings

Check that `user.shops[0].shopDetails` contains:

- `address` (full address details)
- `contact` (phone, email, website)
- `logoUrl` (for branding)
- `brandColorPrimary` (for UI theming)
- `operatingHours` (business hours)

### ✅ Hardware Peripherals

Check additional hardware in `hardwareConfig`:

- Barcode scanner configuration
- Cash drawer settings
- Payment terminal details
- POS terminal identification

---

## Frontend Integration Example

### JavaScript/TypeScript

```javascript
// Login and extract shop configuration
async function loginAndConfigurePOS(username, password) {
  const response = await fetch("http://localhost:5000/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ identifier: username, password }),
  });

  const data = await response.json();

  // Get primary shop
  const primaryShop = data.user.shops[0];
  const shopDetails = primaryShop.shopDetails;

  // Configure printer
  const printer = {
    name: shopDetails.hardwareConfig.receiptPrinterName,
    ip: shopDetails.hardwareConfig.receiptPrinterIpAddress,
    port: shopDetails.hardwareConfig.receiptPrinterPort,
    connectionType: shopDetails.hardwareConfig.receiptPrinterConnectionType,
  };

  console.log("Configuring printer:", printer);

  // Configure receipt format
  const receiptSettings = {
    shopName: shopDetails.receiptConfig.receiptShopName,
    width: shopDetails.receiptConfig.receiptWidth,
    showLogo: shopDetails.receiptConfig.showLogoOnReceipt,
    headerText: shopDetails.receiptConfig.headerText,
  };

  console.log("Receipt settings:", receiptSettings);

  // Configure branding
  document.documentElement.style.setProperty(
    "--primary-color",
    shopDetails.brandColorPrimary
  );

  // Store shop configuration
  localStorage.setItem("shopConfig", JSON.stringify(shopDetails));
  localStorage.setItem("accessToken", data.accessToken);

  return data;
}
```

### React/Vue Component

```javascript
// In your React/Vue component
const { user } = await loginAndConfigurePOS("username", "password");

// Access any shop detail
const shops = user.shops;
shops.forEach((shop) => {
  console.log(`Shop: ${shop.shopDetails.shopName}`);
  console.log(`Printer: ${shop.shopDetails.hardwareConfig.receiptPrinterName}`);
  console.log(`Address: ${shop.shopDetails.address.street}`);
});
```

---

## Error Responses

### Invalid Credentials (401)

```json
{
  "error": "Invalid credentials"
}
```

### Account Locked (401)

```json
{
  "error": "Account is locked until 2025-10-26 10:00:00"
}
```

### Account Inactive (401)

```json
{
  "error": "Account is inactive"
}
```

---

## Performance Notes

- **Single Query**: All shop details loaded in one database query
- **No N+1 Problem**: Eager loading with `.Include()` prevents multiple queries
- **Response Size**: ~5-10KB per shop (acceptable for modern networks)
- **Cache Friendly**: Frontend can cache shop details between sessions

---

## Troubleshooting

### Shop Details is Null

- Ensure user has active shop membership
- Check `shopId` was provided during registration
- Verify shop exists in database

### Missing Hardware Config

- Check if shop has hardware configuration set
- Use shop management endpoints to update hardware config

### Printer Settings Not Applied

- Verify `hardwareConfig.receiptPrinterName` is not null
- Check printer connection type matches your setup
- Validate IP address and port for network printers

---

**Last Updated**: October 25, 2025
**API Version**: 1.0
**Status**: ✅ Ready for Testing
