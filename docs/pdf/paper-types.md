# PDF Receipt - Paper Type Support

## ✅ Implementation Complete

The PDF receipt system now supports **4 different paper types** for various printing scenarios:

### Supported Paper Types

| Paper Type      | Dimensions   | Use Case                         | Margin |
| --------------- | ------------ | -------------------------------- | ------ |
| **A4**          | 210 x 297 mm | Standard document printing       | 20pt   |
| **A5**          | 148 x 210 mm | Compact receipts (default)       | 15pt   |
| **Thermal80mm** | 80 x 200 mm  | Standard thermal receipt printer | 5pt    |
| **Thermal58mm** | 58 x 200 mm  | Compact thermal receipt printer  | 3pt    |

## How to Use

### 1. Via API Query Parameter

```http
GET /api/pdf/receipt/{orderId}?paperType=A4
GET /api/pdf/receipt/{orderId}?paperType=A5
GET /api/pdf/receipt/{orderId}?paperType=Thermal80mm
GET /api/pdf/receipt/{orderId}?paperType=Thermal58mm
```

**Example:**

```bash
# Generate A4 receipt
curl http://localhost:5135/api/pdf/receipt/SO-20251105213131-5798?paperType=A4 \
  -H "Authorization: Bearer YOUR_TOKEN" \
  --output receipt-a4.pdf

# Generate 80mm thermal receipt
curl http://localhost:5135/api/pdf/receipt/SO-20251105213131-5798?paperType=Thermal80mm \
  -H "Authorization: Bearer YOUR_TOKEN" \
  --output receipt-thermal.pdf
```

### 2. Via Custom Receipt Request

```json
POST /api/pdf/receipt/custom
{
  "orderNumber": "SO-20251105213131-5798",
  "shopName": "HK Pharmacy",
  "paperType": "Thermal80mm",
  ...
}
```

### 3. Via Shop Configuration (Default)

Set the default paper type in shop settings:

```json
PUT /api/shops/{shopId}
{
  "receiptConfig": {
    "paperType": "Thermal80mm"
  }
}
```

The system will use this default when no `paperType` parameter is provided.

## Paper Type Specifications

### A4 (Standard Document)

- **Size**: 210 x 297 mm (8.27 x 11.69 inches)
- **Best for**: Formal invoices, detailed receipts
- **Margin**: 20pt
- **Use case**: Office printing, customer copies

### A5 (Compact Receipt) - DEFAULT

- **Size**: 148 x 210 mm (5.83 x 8.27 inches)
- **Best for**: Standard receipts, half-page documents
- **Margin**: 15pt
- **Use case**: General purpose receipts

### Thermal80mm (Standard Thermal)

- **Size**: 80 x 200 mm (3.15 x 7.87 inches)
- **Best for**: POS thermal printers
- **Margin**: 5pt
- **Use case**: Restaurant receipts, retail stores
- **Print width**: ~72mm usable width

### Thermal58mm (Compact Thermal)

- **Size**: 58 x 200 mm (2.28 x 7.87 inches)
- **Best for**: Mobile thermal printers
- **Margin**: 3pt
- **Use case**: Mobile POS, food delivery
- **Print width**: ~52mm usable width

## DTO Changes

### ReceiptDto

```csharp
public record ReceiptDto
{
    // ... existing properties
    public PaperType PaperType { get; init; } = PaperType.A5;
}
```

### PaperType Enum

```csharp
public enum PaperType
{
    A4,           // 210 x 297 mm
    A5,           // 148 x 210 mm (default)
    Thermal80mm,  // 80 x 200 mm
    Thermal58mm   // 58 x 200 mm
}
```

## Shop Configuration

### ReceiptConfiguration Updates

```csharp
public class ReceiptConfiguration
{
    // ... existing properties
    public string PaperType { get; set; } = "A5";
}
```

## Examples

### Test with Different Paper Sizes

**English Receipt on A4:**

```http
GET http://localhost:5135/api/pdf/receipt/SO-20251105213131-5798?paperType=A4&language=en-US
Authorization: Bearer YOUR_TOKEN
```

**Arabic Receipt on 80mm Thermal:**

```http
GET http://localhost:5135/api/pdf/receipt/SO-20251105213131-5798?paperType=Thermal80mm&language=ar
Authorization: Bearer YOUR_TOKEN
```

**Custom Receipt with Thermal 58mm:**

```json
POST http://localhost:5135/api/pdf/receipt/custom
Content-Type: application/json

{
  "orderNumber": "SO-TEST-001",
  "shopName": "Quick Pharmacy",
  "paperType": "Thermal58mm",
  "language": "en-US",
  "items": [...],
  ...
}
```

## Testing

Use the updated `tests/pdf-receipt.http` file which includes examples for all paper types:

1. **Test 2a**: A4 paper receipt
2. **Test 2b**: 80mm thermal receipt
3. **Test 2c**: 58mm thermal receipt (Arabic)
4. **Test 3a**: Custom A4 receipt
5. **Test 3b**: Custom 80mm thermal receipt

## Priority Order

When generating a receipt, paper type is determined in this order:

1. **Query Parameter**: `?paperType=A4` (highest priority)
2. **Shop Configuration**: `Shop.ReceiptConfig.PaperType`
3. **Default**: `A5` (fallback)

## Layout Adaptation

The PDF service automatically adapts the layout based on paper type:

- **A4/A5**: More spacing, larger fonts, suitable for document printing
- **Thermal**: Compact layout, smaller margins, optimized for thermal printers
- **Font sizes**: Automatically adjusted for readability on each format
- **Margins**: Reduced for thermal formats to maximize print area

## Benefits

✅ **Flexibility**: Support any printer type
✅ **Shop-Specific**: Each shop can set their preferred default
✅ **Override**: Per-request paper type selection
✅ **Optimized**: Layouts adapted for each format
✅ **Thermal Ready**: Direct support for POS thermal printers
✅ **Mobile Compatible**: 58mm support for mobile printers

## Future Enhancements

- Auto-detect printer type
- Custom paper dimensions
- Print queue management
- Direct thermal printer integration (ESC/POS commands)
- Paper roll auto-cut markers

## Notes

- Thermal sizes (80mm, 58mm) are designed for continuous paper
- Height is set to 200mm but will auto-expand based on content
- Thermal printers may require additional driver configuration
- For best results with thermal printers, use the appropriate paper type setting
