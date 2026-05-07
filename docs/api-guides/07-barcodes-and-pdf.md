# 7. Barcodes & PDF Receipts

**What it's for**: the physical-world side — labels you print, barcodes you scan, receipts you hand customers.

**Use this when**:
- Printing labels for new stock → `POST /api/barcodes/generate/barcode`
- The cashier scans a barcode and you need to identify a drug → `POST /api/barcodes/scan` (then `/api/barcodes/search` to resolve, or for the till use `/api/inventory/.../pos-items/by-barcode/{barcode}` instead — it returns price + stock too)
- Printing a sale receipt → `GET /api/pdf/receipt/{orderId}`
- Previewing receipt branding before saving config → `POST /api/pdf/receipt/custom`

Two adjacent capabilities that close the loop between catalog, till, and customer:

- **Barcodes / QR codes** — generated from drug data; scannable from images on the cashier side.
- **PDF receipts** — rendered with QuestPDF in multiple paper sizes and languages, using the shop's receipt config.

---

## Part A — Barcodes

### Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| POST | `/api/barcodes/generate/barcode` | public | Render a one-off barcode PNG from arbitrary data |
| POST | `/api/barcodes/generate/qrcode` | public | Render a one-off QR code PNG |
| GET  | `/api/barcodes/drugs/{drugId}` | `ShopAccess` | Drug's barcode + QR (base64 PNGs) |
| GET  | `/api/barcodes/drugs/{drugId}/download/barcode` | `ShopAccess` | Same but as PNG download |
| GET  | `/api/barcodes/drugs/{drugId}/download/qrcode` | `ShopAccess` | QR PNG download |
| GET  | `/api/barcodes/search?barcode=...` | `ShopAccess` | Resolve barcode → drug |
| POST | `/api/barcodes/scan` | `ShopAccess` | Decode an uploaded barcode/QR image |

> ⚠ The two `generate/*` endpoints have no `[Authorize]`. They render images from whatever payload you send — fine for label printing inside a network, but should be rate-limited or moved behind auth before exposing to the internet.

### Generating a one-off barcode

```http
POST /api/barcodes/generate/barcode

{
  "data": "8901234567890",
  "width": 300,
  "height": 100
}
```

Returns `200 OK image/png` — the rendered barcode bytes. Useful for label printing where the data isn't tied to a stored drug.

### Drug barcode + QR

```http
GET /api/barcodes/drugs/DRG-A1B2C3D4E5F6
```

```json
{
  "drugId": "DRG-A1B2C3D4E5F6",
  "brandName": "Amoxil 500",
  "barcode": "8901234567890",
  "barcodeImageBase64": "iVBORw0KGgoAAAANS…",
  "qrCodeImageBase64": "iVBORw0KGgoAAAANS…"
}
```

The QR encodes a small JSON blob (drugId, barcode, brandName, genericName) so a scan-to-app flow round-trips without a server hit.

### Search by barcode (catalog lookup)

```http
GET /api/barcodes/search?barcode=8901234567890
```

Returns the same shape as above. **For till lookup that includes price + stock, use `/api/inventory/shops/{shopId}/pos-items/by-barcode/{barcode}` instead** (see [06 — Cashier](./06-cashier-pos-checkout.md)).

### Decoding an image

```http
POST /api/barcodes/scan
Content-Type: multipart/form-data

file=<barcode.png>
```

Response — `ScanResultDto` (not the same shape as the lookup endpoints):

```json
{
  "success": true,
  "data": "8901234567890",
  "message": "Successfully decoded"
}
```

If nothing decodable is in the image:

```json
{ "success": false, "data": null, "message": "No barcode or QR code found in image" }
```

This endpoint **only decodes**. It doesn't resolve the barcode to a drug. To match against the catalog, take `data` and pass it to `GET /api/barcodes/search?barcode=…` (or `GET /api/inventory/shops/{shopId}/pos-items/by-barcode/{barcode}` for the till). Powered by ZXing.Net + SkiaSharp.

---

## Part B — PDF Receipts

### Endpoints

| Method | Route | Auth | Purpose |
|--------|-------|------|---------|
| GET  | `/api/pdf/receipt/{orderId}?language=&paperType=` | bearer | Receipt for a real sales order |
| POST | `/api/pdf/receipt/custom` | public (`AllowAnonymous`) | Render a receipt from a payload (e.g. previews) |

Defaults baked into `ReceiptDto`:
- `paperType` defaults to **`A5`** (not Thermal80mm). Pass `?paperType=Thermal80mm` if your printer needs it.
- `language` defaults to **`en-US`**.
- `currency` defaults to **`"IQD"`** (Iraqi Dinar). Override per receipt or set the shop's currency.

### Generate a receipt for a completed order

```http
GET /api/pdf/receipt/SO-uuid?language=en-US&paperType=Thermal80mm
Authorization: Bearer …
```

Response is `application/pdf`. The handler:

1. Loads the `SalesOrder` (must exist and belong to a shop the caller can access).
2. Loads the shop's `ReceiptConfig` (logo, header/footer text, VAT lines, license number, etc.).
3. Composes a `ReceiptDto` with order + shop + items merged.
4. Renders via QuestPDF using the chosen paper template.

### Supported paper types

| Value | Use case |
|-------|----------|
| `A4` | Office printer |
| `A5` | Smaller page printer |
| `Thermal80mm` | Standard POS thermal printer |
| `Thermal58mm` | Smaller mobile/portable thermal printer |

For exact widths, margins, and how each layout is built see [`../pdf/paper-types.md`](../pdf/paper-types.md).

### Supported languages

`language` query takes a culture code, e.g. `en-US`, `ar`. Layout flips to RTL for Arabic. Default falls back to the shop's configured `language`.

### Receipt content (ReceiptDto)

What ends up on every receipt, drawn from three sources:

| Section | Source |
|---------|--------|
| Shop header (`shopName`, `shopAddress`, `shopPhone`, `shopEmail`, `logoUrl`) | shop profile |
| Compliance lines (`vatRegistrationNumber`, `pharmacyLicenseNumber`) | shop receipt config |
| Toggles (`showLogo`, `showQrCode`, `showTaxBreakdown`, `showVatNumber`, `showPharmacyLicense`) | shop receipt config |
| Free-form (`headerText`, `footerText`) | shop receipt config |
| Order body (`orderNumber`, `orderDate`, `customerName`, `salespersonName`, `paymentMethod`, items) | sales order |
| Money (`subtotal`, `taxAmount`, `taxRate`, `total`, `amountPaid`, `change`, `currency`) | sales order |

Each `ReceiptItemDto` row has just: `itemName`, `quantity`, `unitPrice`, `total`, `requiresPrescription` (bool — drives a "Rx" mark on the printed line).

### Custom render (no order needed)

For preview / template-tweaking screens:

```http
POST /api/pdf/receipt/custom
Content-Type: application/json

{
  "orderNumber": "PREVIEW-001",
  "shopName": "Main Branch Pharmacy",
  "logoUrl": "https://cdn.pharma.com/logo.png",
  "showLogo": true,
  "language": "en-US",
  "paperType": "Thermal80mm",
  "items": [
    {
      "itemName": "Amoxil 500 (Box)",
      "quantity": 2,
      "unitPrice": 25.00,
      "total": 47.50,
      "requiresPrescription": true
    }
  ],
  "subtotal": 50.00,
  "taxAmount": 5.00,
  "total": 52.50,
  "amountPaid": 60.00,
  "change": 7.50,
  "currency": "USD"
}
```

Returns the PDF — no DB write. The handler validates `orderNumber` is non-empty and `items` is non-empty (else `400`).

For deeper architecture (component tree, rendering pipeline, how to add a new paper layout) see [`../pdf/system.md`](../pdf/system.md). For a 5-minute walkthrough see [`../pdf/quickstart.md`](../pdf/quickstart.md).

---

## Common pitfalls

- **Wrong barcode endpoint at the till** — `/api/barcodes/search` returns catalog data only. Use the inventory POS-items endpoint to get current price and on-hand stock.
- **Receipt missing logo / VAT line** — those are toggles in `receiptConfig`. Update via `PUT /api/shops/{id}/receipt-config` (see [02 — Shops](./02-shops-and-members.md)).
- **Thermal printer truncating columns** — use `Thermal58mm` instead of `Thermal80mm`; 80mm assumes wider paper than some mobile printers ship with.
- **Anonymous access on `/receipt/custom`** — designed for previews. Don't put a public-facing form in front of it that accepts arbitrary `shopName`/`logoUrl` without sanitisation.

---

## Best practices

### Security
- **Put `[Authorize]` on `/api/barcodes/generate/barcode` and `/qrcode`** before exposing the API to the internet. They're CPU-intensive image generators that accept arbitrary data — a perfect denial-of-service target.
- **Lock down `POST /api/pdf/receipt/custom`.** It's `[AllowAnonymous]` for "testing purposes" — fine in dev, dangerous in prod. An attacker can render arbitrary text claiming to be a real shop, complete with logo URL of their choice.
- **Validate `logoUrl` before rendering.** It's fetched server-side by QuestPDF; an attacker-controlled URL is a server-side request forgery (SSRF) primitive. At minimum, restrict to a known CDN host pattern.
- **Rate-limit barcode generation per IP/user.** PNG encoding is expensive; a small loop will saturate a CPU.
- **Don't include full prescription details in receipts** that may be photographed and shared. Use just enough to satisfy compliance (drug name, qty, Rx flag) — `requiresPrescription` toggles the visual mark.

### Performance
- **Cache `/api/barcodes/drugs/{drugId}` responses on the client.** Both barcode and QR images are deterministic for a given drug. Bust the cache only when the drug's `barcode` field changes.
- **Generate labels in batch.** If you're printing 100 labels, render them all server-side into one PDF page set rather than 100 separate calls.
- **PDF receipts are CPU-bound (QuestPDF) but stateless.** Horizontal scaling is fine; don't put heavy in-memory caches on the receipt path.
- **`Thermal58mm` and `Thermal80mm` produce smaller PDFs** than A4/A5 — use them when delivering to mobile printers over slow networks.

### Correctness
- **`POST /api/barcodes/scan` returns `{ success, data, message }` only.** It does *not* identify a drug. Always pair it with `GET /api/barcodes/search?barcode=…` (catalog) or `GET /api/inventory/.../pos-items/by-barcode/{barcode}` (till) to resolve.
- **The PDF endpoint defaults to `paperType = A5`, `currency = "IQD"`, `language = "en-US"`.** If your business uses different defaults, *always* pass them explicitly — the shop's `receiptConfig.receiptLanguage` and `receiptWidth` are not currently merged into the receipt automatically.
- **`/receipt/{orderId}` reflects the order at fetch time**, not at completion time. Re-printing an old refunded order shows current totals, not the original — keep an immutable copy elsewhere if regulator wants point-in-time receipts.
- **QR codes embed JSON** (drugId, barcode, brandName, genericName). If you change the schema, mobile apps that decode them will break — version it.

### Clean code
- **At the till, never use `/api/barcodes/search`** — use `/api/inventory/shops/{shopId}/pos-items/by-barcode/{barcode}` instead. The inventory endpoint returns drug + price + stock + nearest expiry in one call.
- **Use `/api/barcodes/search` from inventory tools** (label printing, stock-in workflows) where you don't yet care about a specific shop's price.
- **Build the receipt template once.** Toggles like `showLogo`, `showQrCode`, `showTaxBreakdown`, `showVatNumber` are designed to express variants of one template — don't fork separate code paths per shop.
- **Pass `language` and `paperType` from the shop's `receiptConfig`/`hardwareConfig`** rather than hard-coding them per call. The till already has those values from login.

## You're done

You've now seen every major API in the system end to end. From here:

- **Domain deep-dives**: [`../drugs/`](../drugs/), [`../packaging/`](../packaging/), [`../pricing/`](../pricing/), [`../pdf/`](../pdf/)
- **Conventions**: [`../CODING_STANDARDS.md`](../CODING_STANDARDS.md)
- **Setup**: [`../../SECURITY_SETUP.md`](../../SECURITY_SETUP.md)
- **Live spec**: <http://localhost:5000/swagger>
