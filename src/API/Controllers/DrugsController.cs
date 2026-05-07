using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Application.Drugs.Queries.GetDrug;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugDetail;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugList;
using pos_system_api.Core.Application.Drugs.Queries.GetDrugListEnhanced;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for drug operations.
/// Read paths dispatch through MediatR; the write path (CreateDrug) still
/// uses ApplicationDbContext directly and will be moved to a CreateDrugCommand
/// handler in a future change (see docs/CONTROLLER_REFACTOR_BACKLOG.md).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DrugsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DrugsController> _logger;

    public DrugsController(
        IMediator mediator,
        ApplicationDbContext context,
        ILogger<DrugsController> logger)
    {
        _mediator = mediator;
        _context = context;
        _logger = logger;
    }

    /// <summary>Create a new drug in the catalog.</summary>
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DrugDto>> CreateDrug([FromBody] CreateDrugDto dto)
    {
        try
        {
            var result = await CreateDrugInternalAsync(dto, HttpContext.RequestAborted);
            return CreatedAtAction(nameof(GetDrug), new { id = result.DrugId }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get a single drug by ID.</summary>
    [HttpGet("{id}")]
    [AllowAnonymous] // Drug catalog is public
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DrugDto>> GetDrug(string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDrugQuery(id), cancellationToken);
        return result == null
            ? NotFound(new { error = $"Drug with ID '{id}' not found" })
            : Ok(result);
    }

    /// <summary>Get list of drugs with pagination.</summary>
    [HttpGet]
    [AllowAnonymous] // Drug catalog is public
    [ProducesResponseType(typeof(PagedResult<DrugDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DrugDto>>> GetDrugs(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20)
    {
        var result = await _mediator.Send(new GetDrugListQuery(page, limit), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get enhanced drug list with images, prices, stock, and category info
    /// (lightweight, intended for catalog browsing).
    /// </summary>
    [HttpGet("browse")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<DrugListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DrugListItemDto>>> GetDrugsBrowse(
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? inStock = null)
    {
        var result = await _mediator.Send(
            new GetDrugListEnhancedQuery(page, limit, searchTerm, category, inStock),
            cancellationToken);
        return Ok(result);
    }

    /// <summary>Get full drug details including inventory across all shops.</summary>
    [HttpGet("{id}/detail")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DrugDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DrugDetailDto>> GetDrugDetail(
        string id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetDrugDetailQuery(id), cancellationToken);
        return result == null
            ? NotFound(new { error = $"Drug with ID '{id}' not found" })
            : Ok(result);
    }

    // ----- write path (CreateDrug) — to be extracted into CreateDrugCommand in a future step -----

    private async Task<DrugDto> CreateDrugInternalAsync(
        CreateDrugDto dto,
        CancellationToken cancellationToken)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (string.IsNullOrWhiteSpace(dto.BrandName))
            throw new ArgumentException("BrandName is required.", nameof(dto.BrandName));

        if (string.IsNullOrWhiteSpace(dto.GenericName))
            throw new ArgumentException("GenericName is required.", nameof(dto.GenericName));

        if (string.IsNullOrWhiteSpace(dto.CategoryId))
            throw new ArgumentException("CategoryId is required.", nameof(dto.CategoryId));

        if (dto.PackagingInfo == null)
            throw new ArgumentException("PackagingInfo is required.", nameof(dto.PackagingInfo));

        if (dto.PackagingInfo.PackagingLevels == null
            || dto.PackagingInfo.PackagingLevels.Count == 0)
        {
            throw new ArgumentException(
                "PackagingLevels must contain at least one level.",
                nameof(dto.PackagingInfo.PackagingLevels));
        }

        var desiredDrugId = dto.DrugId?.Trim();
        if (!string.IsNullOrWhiteSpace(desiredDrugId))
        {
            var existingDrug = await _context.Drugs
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DrugId == desiredDrugId, cancellationToken);

            if (existingDrug != null)
                throw new InvalidOperationException(
                    $"Drug with ID '{desiredDrugId}' already exists.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var trimmedBarcode = dto.Barcode.Trim();
            var existingBarcode = await _context.Drugs
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Barcode == trimmedBarcode, cancellationToken);

            if (existingBarcode != null)
                throw new InvalidOperationException(
                    $"Barcode '{trimmedBarcode}' is already assigned to another drug.");
        }

        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId.Trim(), cancellationToken);

        if (category == null)
            throw new InvalidOperationException($"Category '{dto.CategoryId}' does not exist.");

        var drug = new Drug
        {
            DrugId = !string.IsNullOrWhiteSpace(desiredDrugId) ? desiredDrugId : GenerateDrugId(),
            Barcode = dto.Barcode?.Trim() ?? string.Empty,
            BarcodeType = string.IsNullOrWhiteSpace(dto.BarcodeType) ? "EAN-13" : dto.BarcodeType.Trim(),
            BrandName = dto.BrandName.Trim(),
            GenericName = dto.GenericName.Trim(),
            Manufacturer = dto.Manufacturer?.Trim() ?? string.Empty,
            OriginCountry = dto.OriginCountry?.Trim() ?? string.Empty,
            CategoryName = category.Name,
            CategoryId = category.CategoryId,
            ImageUrls = dto.ImageUrls?.Where(url => !string.IsNullOrWhiteSpace(url))
                .Select(url => url.Trim())
                .ToList() ?? new List<string>(),
            Description = dto.Description ?? string.Empty,
            SideEffects = dto.SideEffects ?? new List<string>(),
            InteractionNotes = dto.InteractionNotes ?? new List<string>(),
            Tags = dto.Tags ?? new List<string>(),
            RelatedDrugs = dto.RelatedDrugs ?? new List<string>(),
            Formulation = MapFormulation(dto.Formulation),
            BasePricing = MapBasePricing(dto.BasePricing),
            Regulatory = MapRegulatory(dto.Regulatory),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User?.Identity?.Name ?? "system",
        };

        drug.PackagingInfo = BuildPackagingInfo(dto.PackagingInfo);

        _context.Drugs.Add(drug);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created drug {DrugId} with barcode {Barcode}.",
            drug.DrugId, drug.Barcode);

        return MapToDto(drug);
    }

    private static DrugDto MapToDto(Drug drug) => new()
    {
        DrugId = drug.DrugId,
        Barcode = drug.Barcode,
        BarcodeType = drug.BarcodeType,
        BrandName = drug.BrandName,
        GenericName = drug.GenericName,
        Manufacturer = drug.Manufacturer,
        OriginCountry = drug.OriginCountry,
        CategoryId = drug.CategoryId,
        Category = drug.Category?.Name ?? drug.CategoryName,
        ImageUrls = drug.ImageUrls,
        Description = drug.Description,
        SideEffects = drug.SideEffects,
        InteractionNotes = drug.InteractionNotes,
        Tags = drug.Tags,
        RelatedDrugs = drug.RelatedDrugs,
        Formulation = drug.Formulation,
        BasePricing = drug.BasePricing,
        Regulatory = drug.Regulatory,
        PackagingInfo = drug.PackagingInfo,
        CreatedAt = drug.CreatedAt,
        CreatedBy = drug.CreatedBy,
        LastUpdated = drug.LastUpdated,
        UpdatedBy = drug.UpdatedBy,
    };

    private static Formulation MapFormulation(CreateFormulationDto? source)
    {
        if (source == null)
        {
            return new Formulation();
        }

        return new Formulation
        {
            Form = source.Form?.Trim() ?? string.Empty,
            Strength = source.Strength?.Trim() ?? string.Empty,
            RouteOfAdministration = source.RouteOfAdministration?.Trim() ?? string.Empty,
        };
    }

    private static BasePricing MapBasePricing(CreateBasePricingDto? source)
    {
        if (source == null)
        {
            return new BasePricing();
        }

        return new BasePricing
        {
            SuggestedRetailPrice = source.SuggestedRetailPrice,
            Currency = string.IsNullOrWhiteSpace(source.Currency) ? "USD" : source.Currency.Trim(),
            SuggestedTaxRate = source.SuggestedTaxRate,
            LastPriceUpdate = DateTime.UtcNow,
        };
    }

    private static Regulatory MapRegulatory(CreateRegulatoryDto? source)
    {
        if (source == null)
        {
            return new Regulatory();
        }

        return new Regulatory
        {
            IsPrescriptionRequired = source.IsPrescriptionRequired,
            IsHighRisk = source.IsHighRisk,
            DrugAuthorityNumber = source.DrugAuthorityNumber?.Trim() ?? string.Empty,
            ApprovalDate = source.ApprovalDate == default ? DateTime.UtcNow : source.ApprovalDate,
            ControlSchedule = source.ControlSchedule?.Trim() ?? string.Empty,
        };
    }

    private static PackagingInfo BuildPackagingInfo(CreatePackagingInfoDto source)
    {
        var packagingInfo = new PackagingInfo(
            source.UnitType,
            source.BaseUnit?.Trim() ?? string.Empty,
            source.BaseUnitDisplayName?.Trim() ?? string.Empty,
            source.IsSubdivisible);

        foreach (var level in source.PackagingLevels.OrderBy(l => l.LevelNumber))
        {
            var packagingLevel = new PackagingLevel(
                packagingLevelId: string.IsNullOrWhiteSpace(level.PackagingLevelId)
                    ? null
                    : level.PackagingLevelId.Trim(),
                levelNumber: level.LevelNumber,
                unitName: level.UnitName?.Trim() ?? string.Empty,
                baseUnitQuantity: level.BaseUnitQuantity,
                isSellable: level.IsSellable,
                isDefault: level.IsDefault,
                isBreakable: level.IsBreakable,
                barcode: string.IsNullOrWhiteSpace(level.Barcode) ? null : level.Barcode.Trim(),
                minimumSaleQuantity: level.MinimumSaleQuantity,
                parentPackagingLevelId: string.IsNullOrWhiteSpace(level.ParentPackagingLevelId)
                    ? null
                    : level.ParentPackagingLevelId.Trim(),
                quantityPerParent: level.QuantityPerParent);

            packagingInfo.AddPackagingLevel(packagingLevel);
        }

        var validation = packagingInfo.Validate();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Invalid packaging configuration: {string.Join("; ", validation.Errors)}");
        }

        return packagingInfo;
    }

    private static string GenerateDrugId() =>
        $"DRG-{Guid.NewGuid():N}".Substring(0, 12).ToUpperInvariant();
}
