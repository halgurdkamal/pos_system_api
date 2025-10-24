using Microsoft.Extensions.Logging;
using SkiaSharp;
using pos_system_api.Core.Application.Common.Interfaces;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp.Rendering;

namespace pos_system_api.Infrastructure.Services;

public class BarcodeService : IBarcodeService
{
    private readonly ILogger<BarcodeService> _logger;

    public BarcodeService(ILogger<BarcodeService> logger)
    {
        _logger = logger;
    }

    public byte[] GenerateBarcode(string data, int width = 300, int height = 100)
    {
        try
        {
            var writer = new ZXing.SkiaSharp.BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 10,
                    PureBarcode = false
                }
            };

            using var bitmap = writer.Write(data);
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            
            return encoded.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate barcode for data: {Data}", data);
            throw new InvalidOperationException("Failed to generate barcode", ex);
        }
    }

    public byte[] GenerateQRCode(string data, int width = 300, int height = 300)
    {
        try
        {
            var writer = new ZXing.SkiaSharp.BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 10
                }
            };

            using var bitmap = writer.Write(data);
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            
            return encoded.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate QR code for data: {Data}", data);
            throw new InvalidOperationException("Failed to generate QR code", ex);
        }
    }

    public string? DecodeBarcode(byte[] imageData)
    {
        try
        {
            using var stream = new MemoryStream(imageData);
            using var bitmap = SKBitmap.Decode(stream);
            
            if (bitmap == null)
            {
                _logger.LogWarning("Failed to decode image data");
                return null;
            }

            var reader = new ZXing.SkiaSharp.BarcodeReader
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    PossibleFormats = new[]
                    {
                        BarcodeFormat.QR_CODE,
                        BarcodeFormat.CODE_128,
                        BarcodeFormat.CODE_39,
                        BarcodeFormat.EAN_13,
                        BarcodeFormat.EAN_8,
                        BarcodeFormat.UPC_A,
                        BarcodeFormat.UPC_E
                    }
                }
            };

            var result = reader.Decode(bitmap);
            
            if (result == null)
            {
                _logger.LogWarning("No barcode found in image");
                return null;
            }

            _logger.LogInformation("Successfully decoded barcode: {Result}", result.Text);
            return result.Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decode barcode from image");
            return null;
        }
    }
}
