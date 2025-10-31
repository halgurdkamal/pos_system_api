using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pos_system_api.Core.Application.Common.Interfaces;

namespace pos_system_api.API.Controllers;

// DTOs
public class BarcodeRequestDto
{
    public string Data { get; set; } = string.Empty;
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 100;
}

public class QRCodeRequestDto
{
    public string Data { get; set; } = string.Empty;
    public int Width { get; set; } = 300;
    public int Height { get; set; } = 300;
}

public class DrugBarcodeDto
{
    public string DrugId { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string BarcodeImageBase64 { get; set; } = string.Empty;
    public string QRCodeImageBase64 { get; set; } = string.Empty;
}

public class ScanResultDto
{
    public bool Success { get; set; }
    public string? Data { get; set; }
    public string? Message { get; set; }
}

// Commands & Queries
public record GenerateDrugBarcodeQuery(string DrugId) : IRequest<DrugBarcodeDto>;
public record ScanBarcodeCommand(byte[] ImageData) : IRequest<ScanResultDto>;
public record SearchByBarcodeQuery(string Barcode) : IRequest<DrugBarcodeDto?>;

// Handlers
public class GenerateDrugBarcodeHandler : IRequestHandler<GenerateDrugBarcodeQuery, DrugBarcodeDto>
{
    private readonly IDrugRepository _drugRepository;
    private readonly IBarcodeService _barcodeService;

    public GenerateDrugBarcodeHandler(IDrugRepository drugRepository, IBarcodeService barcodeService)
    {
        _drugRepository = drugRepository;
        _barcodeService = barcodeService;
    }

    public async Task<DrugBarcodeDto> Handle(GenerateDrugBarcodeQuery request, CancellationToken cancellationToken)
    {
        var drug = await _drugRepository.GetByIdAsync(request.DrugId, cancellationToken);
        if (drug == null)
            throw new KeyNotFoundException($"Drug {request.DrugId} not found");

        // Use existing barcode or generate one
        var barcodeData = !string.IsNullOrEmpty(drug.Barcode) ? drug.Barcode : drug.Id;

        // Generate barcode image
        var barcodeImage = _barcodeService.GenerateBarcode(barcodeData, 400, 120);

        // Generate QR code with drug details (JSON format)
        var qrData = System.Text.Json.JsonSerializer.Serialize(new
        {
            drugId = drug.Id,
            barcode = barcodeData,
            brandName = drug.BrandName,
            genericName = drug.GenericName
        });
        var qrCodeImage = _barcodeService.GenerateQRCode(qrData, 300, 300);

        return new DrugBarcodeDto
        {
            DrugId = drug.Id,
            BrandName = drug.BrandName,
            Barcode = barcodeData,
            BarcodeImageBase64 = Convert.ToBase64String(barcodeImage),
            QRCodeImageBase64 = Convert.ToBase64String(qrCodeImage)
        };
    }
}

public class ScanBarcodeHandler : IRequestHandler<ScanBarcodeCommand, ScanResultDto>
{
    private readonly IBarcodeService _barcodeService;

    public ScanBarcodeHandler(IBarcodeService barcodeService) => _barcodeService = barcodeService;

    public Task<ScanResultDto> Handle(ScanBarcodeCommand request, CancellationToken cancellationToken)
    {
        var result = _barcodeService.DecodeBarcode(request.ImageData);

        if (result == null)
        {
            return Task.FromResult(new ScanResultDto
            {
                Success = false,
                Message = "No barcode or QR code found in image"
            });
        }

        return Task.FromResult(new ScanResultDto
        {
            Success = true,
            Data = result,
            Message = "Successfully decoded"
        });
    }
}

public class SearchByBarcodeHandler : IRequestHandler<SearchByBarcodeQuery, DrugBarcodeDto?>
{
    private readonly IDrugRepository _drugRepository;
    private readonly IBarcodeService _barcodeService;

    public SearchByBarcodeHandler(IDrugRepository drugRepository, IBarcodeService barcodeService)
    {
        _drugRepository = drugRepository;
        _barcodeService = barcodeService;
    }

    public async Task<DrugBarcodeDto?> Handle(SearchByBarcodeQuery request, CancellationToken cancellationToken)
    {
        var drug = await _drugRepository.GetByBarcodeAsync(request.Barcode, cancellationToken);
        if (drug == null)
            return null;

        // Generate images
        var barcodeImage = _barcodeService.GenerateBarcode(drug.Barcode ?? drug.Id, 400, 120);
        var qrData = System.Text.Json.JsonSerializer.Serialize(new
        {
            drugId = drug.Id,
            barcode = drug.Barcode ?? drug.Id,
            brandName = drug.BrandName,
            genericName = drug.GenericName
        });
        var qrCodeImage = _barcodeService.GenerateQRCode(qrData, 300, 300);

        return new DrugBarcodeDto
        {
            DrugId = drug.Id,
            BrandName = drug.BrandName,
            Barcode = drug.Barcode ?? drug.Id,
            BarcodeImageBase64 = Convert.ToBase64String(barcodeImage),
            QRCodeImageBase64 = Convert.ToBase64String(qrCodeImage)
        };
    }
}

// Controller
[ApiController]
[Route("api/barcodes")]
[Produces("application/json")]
public class BarcodesController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly IBarcodeService _barcodeService;

    public BarcodesController(IMediator mediator, IBarcodeService barcodeService)
    {
        _mediator = mediator;
        _barcodeService = barcodeService;
    }

    /// <summary>
    /// Generate a barcode image from text/data
    /// </summary>
    [HttpPost("generate/barcode")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GenerateBarcode([FromBody] BarcodeRequestDto request)
    {
        var imageBytes = _barcodeService.GenerateBarcode(request.Data, request.Width, request.Height);
        return File(imageBytes, "image/png");
    }

    /// <summary>
    /// Generate a QR code image from text/data
    /// </summary>
    [HttpPost("generate/qrcode")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GenerateQRCode([FromBody] QRCodeRequestDto request)
    {
        var imageBytes = _barcodeService.GenerateQRCode(request.Data, request.Width, request.Height);
        return File(imageBytes, "image/png");
    }

    /// <summary>
    /// Generate barcode and QR code for a specific drug
    /// </summary>
    [HttpGet("drugs/{drugId}")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(DrugBarcodeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DrugBarcodeDto>> GetDrugBarcode(string drugId)
    {
        try
        {
            var result = await _mediator.Send(new GenerateDrugBarcodeQuery(drugId));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Search for a drug by scanning its barcode
    /// </summary>
    [HttpGet("search")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(DrugBarcodeDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DrugBarcodeDto>> SearchByBarcode([FromQuery] string barcode)
    {
        var result = await _mediator.Send(new SearchByBarcodeQuery(barcode));
        if (result == null)
            return NotFound(new { error = "Drug not found with barcode" });

        return Ok(result);
    }

    /// <summary>
    /// Scan/decode a barcode or QR code from an uploaded image
    /// </summary>
    [HttpPost("scan")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(typeof(ScanResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScanResultDto>> ScanBarcode(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No image file provided" });

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest(new { error = "File must be an image" });

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        var result = await _mediator.Send(new ScanBarcodeCommand(imageBytes));
        return Ok(result);
    }

    /// <summary>
    /// Download drug barcode as PNG image
    /// </summary>
    [HttpGet("drugs/{drugId}/download/barcode")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadDrugBarcode(string drugId)
    {
        try
        {
            var result = await _mediator.Send(new GenerateDrugBarcodeQuery(drugId));
            var imageBytes = Convert.FromBase64String(result.BarcodeImageBase64);

            var fileName = $"{result.Barcode}_barcode.png";
            return File(imageBytes, "image/png", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download drug QR code as PNG image
    /// </summary>
    [HttpGet("drugs/{drugId}/download/qrcode")]
    [Authorize(Policy = "ShopAccess")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadDrugQRCode(string drugId)
    {
        try
        {
            var result = await _mediator.Send(new GenerateDrugBarcodeQuery(drugId));
            var imageBytes = Convert.FromBase64String(result.QRCodeImageBase64);

            var fileName = $"{result.DrugId}_qrcode.png";
            return File(imageBytes, "image/png", fileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
