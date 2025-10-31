using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Domain.Categories.Entities;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;
using pos_system_api.Infrastructure.Data;

namespace pos_system_api.API.Controllers;

/// <summary>
/// API Controller for drug operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DrugsController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DrugsController> _logger;

    public DrugsController(ApplicationDbContext context, ILogger<DrugsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new drug in the catalog
    /// </summary>
    /// <param name="dto">Drug details including packaging information</param>
    /// <returns>The created drug</returns>
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

    /// <summary>
    /// Get a single drug by ID
    /// </summary>
    /// <param name="id">Drug ID</param>
    /// <returns>Drug details</returns>
    [HttpGet("{id}")]
    [AllowAnonymous] // Drug catalog is public
    [ProducesResponseType(typeof(DrugDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DrugDto>> GetDrug(string id)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var drug = await _context
            .Drugs.Include(d => d.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DrugId == id, cancellationToken);

        if (drug == null)
            return NotFound(new { error = $"Drug with ID '{id}' not found" });

        return Ok(MapToDto(drug));
    }

    /// <summary>
    /// Get list of drugs with pagination
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 20)</param>
    /// <returns>Paginated list of drugs</returns>
    [HttpGet]
    [AllowAnonymous] // Drug catalog is public
    [ProducesResponseType(typeof(PagedResult<DrugDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DrugDto>>> GetDrugs(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20
    )
    {
        var cancellationToken = HttpContext.RequestAborted;
        var pagedDrugs = await GetPagedDrugsAsync(page, limit, cancellationToken);
        EnsurePagedResultConsistency(pagedDrugs);

        var dtoList = pagedDrugs.Data.Select(MapToDto).ToList();

        return Ok(
            new PagedResult<DrugDto>(dtoList, pagedDrugs.Page, pagedDrugs.Limit, pagedDrugs.Total)
        );
    }

    /// <summary>
    /// Get enhanced drug list with images, prices, stock, and category info (LIGHTWEIGHT FOR BROWSING)
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 20)</param>
    /// <param name="searchTerm">Search by name, barcode, or manufacturer</param>
    /// <param name="category">Filter by category</param>
    /// <param name="inStock">Filter by stock availability</param>
    /// <returns>Paginated list with essential drug info</returns>
    [HttpGet("browse")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<DrugListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<DrugListItemDto>>> GetDrugsBrowse(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? inStock = null
    )
    {
        var cancellationToken = HttpContext.RequestAborted;
        var pagedDrugs = await GetPagedDrugsAsync(page, limit, cancellationToken);
        EnsurePagedResultConsistency(pagedDrugs);

        var categories = await _context
            .Categories.AsNoTracking()
            .ToDictionaryAsync(
                c => c.CategoryId,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken
            );

        var items = pagedDrugs.Data.Select(drug => MapToListItemDto(drug, categories)).ToList();
        items = ApplyBrowseFilters(items, searchTerm, category, inStock);

        var response = new PagedResult<DrugListItemDto>
        {
            Data = items,
            Page = page,
            Limit = limit,
            Total = pagedDrugs.Total,
        };

        return Ok(response);
    }

    /// <summary>
    /// Get FULL drug details including inventory across ALL shops (DETAILED VIEW)
    /// </summary>
    /// <param name="id">Drug ID</param>
    /// <returns>Complete drug information with shop inventories</returns>
    [HttpGet("{id}/detail")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DrugDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DrugDetailDto>> GetDrugDetail(string id)
    {
        var cancellationToken = HttpContext.RequestAborted;

        var drug = await _context
            .Drugs.Include(d => d.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DrugId == id, cancellationToken);

        if (drug == null)
            return NotFound(new { error = $"Drug with ID '{id}' not found" });

        var categoryInfo = await _context
            .Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == drug.CategoryId, cancellationToken);

        var shopInventories = await _context
            .ShopInventory.AsNoTracking()
            .Where(inv => inv.DrugId == id)
            .ToListAsync(cancellationToken);

        var shopIds = shopInventories.Select(inv => inv.ShopId).Distinct().ToList();
        var shops = await _context
            .Shops.AsNoTracking()
            .Where(s => shopIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var detailDto = BuildDrugDetailDto(drug, categoryInfo, shopInventories, shops);
        return Ok(detailDto);
    }

    private async Task<DrugDto> CreateDrugInternalAsync(
        CreateDrugDto dto,
        CancellationToken cancellationToken
    )
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

        if (
            dto.PackagingInfo.PackagingLevels == null
            || dto.PackagingInfo.PackagingLevels.Count == 0
        )
            throw new ArgumentException(
                "PackagingLevels must contain at least one level.",
                nameof(dto.PackagingInfo.PackagingLevels)
            );

        var desiredDrugId = dto.DrugId?.Trim();
        if (!string.IsNullOrWhiteSpace(desiredDrugId))
        {
            var existingDrug = await _context
                .Drugs.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DrugId == desiredDrugId, cancellationToken);

            if (existingDrug != null)
                throw new InvalidOperationException(
                    $"Drug with ID '{desiredDrugId}' already exists."
                );
        }

        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var trimmedBarcode = dto.Barcode.Trim();
            var existingBarcode = await _context
                .Drugs.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Barcode == trimmedBarcode, cancellationToken);

            if (existingBarcode != null)
                throw new InvalidOperationException(
                    $"Barcode '{trimmedBarcode}' is already assigned to another drug."
                );
        }

        var category = await _context
            .Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryId == dto.CategoryId.Trim(), cancellationToken);

        if (category == null)
            throw new InvalidOperationException($"Category '{dto.CategoryId}' does not exist.");

        var drug = new Drug
        {
            DrugId = !string.IsNullOrWhiteSpace(desiredDrugId) ? desiredDrugId : GenerateDrugId(),
            Barcode = dto.Barcode?.Trim() ?? string.Empty,
            BarcodeType = string.IsNullOrWhiteSpace(dto.BarcodeType)
                ? "EAN-13"
                : dto.BarcodeType.Trim(),
            BrandName = dto.BrandName.Trim(),
            GenericName = dto.GenericName.Trim(),
            Manufacturer = dto.Manufacturer?.Trim() ?? string.Empty,
            OriginCountry = dto.OriginCountry?.Trim() ?? string.Empty,
            CategoryName = category.Name,
            CategoryId = category.CategoryId,
            ImageUrls =
                dto.ImageUrls?.Where(url => !string.IsNullOrWhiteSpace(url))
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
            drug.DrugId,
            drug.Barcode
        );

        return MapToDto(drug);
    }

    private async Task<PagedResult<Drug>> GetPagedDrugsAsync(
        int page,
        int limit,
        CancellationToken cancellationToken
    )
    {
        if (page <= 0)
            page = 1;

        if (limit <= 0)
            limit = 20;

        var total = await _context.Drugs.CountAsync(cancellationToken);

        var data = await _context
            .Drugs.Include(d => d.Category)
            .AsNoTracking()
            .OrderBy(d => d.BrandName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new PagedResult<Drug>(data, page, limit, total);
    }

    private static void EnsurePagedResultConsistency(PagedResult<Drug> pagedResult)
    {
        var skip = (pagedResult.Page - 1) * pagedResult.Limit;
        var skipBeyondTotal = skip >= pagedResult.Total;

        if (pagedResult.Total > 0 && pagedResult.Data.Count == 0 && !skipBeyondTotal)
        {
            throw new InvalidOperationException(
                "Detected drugs in the catalog but none could be materialized. Verify category references for each drug."
            );
        }
    }

    private static DrugDto MapToDto(Drug drug)
    {
        return new DrugDto
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
    }

    private static DrugListItemDto MapToListItemDto(
        Drug drug,
        IDictionary<string, Category> categoriesById
    )
    {
        categoriesById.TryGetValue(drug.CategoryId, out var categoryInfo);

        return new DrugListItemDto
        {
            DrugId = drug.Id,
            BrandName = drug.BrandName,
            GenericName = drug.GenericName,
            Barcode = drug.Barcode,
            CategoryId = drug.CategoryId,
            Category = categoryInfo?.Name ?? drug.CategoryName,
            CategoryLogoUrl = categoryInfo?.LogoUrl,
            CategoryColorCode = categoryInfo?.ColorCode,
            PrimaryImageUrl = drug.ImageUrls.FirstOrDefault(),
            Manufacturer = drug.Manufacturer,
            Strength = drug.Formulation.Strength,
            Form = drug.Formulation.Form,
            SuggestedRetailPrice = drug.BasePricing.SuggestedRetailPrice,
            WholesalePrice = 0,
            TotalQuantityInStock = 0,
            ShopCount = 0,
            IsAvailable = true,
            RequiresPrescription = drug.Regulatory.IsPrescriptionRequired,
        };
    }

    private static List<DrugListItemDto> ApplyBrowseFilters(
        List<DrugListItemDto> source,
        string? searchTerm,
        string? category,
        bool? inStock
    )
    {
        var items = source;

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var searchLower = searchTerm.ToLowerInvariant();
            items = items
                .Where(d =>
                    d.BrandName.ToLowerInvariant().Contains(searchLower)
                    || d.GenericName.ToLowerInvariant().Contains(searchLower)
                    || d.Barcode.ToLowerInvariant().Contains(searchLower)
                    || d.Manufacturer.ToLowerInvariant().Contains(searchLower)
                )
                .ToList();
        }

        if (!string.IsNullOrEmpty(category))
        {
            items = items
                .Where(d =>
                    d.CategoryId.Equals(category, StringComparison.OrdinalIgnoreCase)
                    || d.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                )
                .ToList();
        }

        if (inStock.HasValue)
        {
            items = items.Where(d => d.IsAvailable == inStock.Value).ToList();
        }

        return items;
    }

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
            source.IsSubdivisible
        );

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
                quantityPerParent: level.QuantityPerParent
            );

            packagingInfo.AddPackagingLevel(packagingLevel);
        }

        var validation = packagingInfo.Validate();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Invalid packaging configuration: {string.Join("; ", validation.Errors)}"
            );
        }

        return packagingInfo;
    }

    private static DrugDetailDto BuildDrugDetailDto(
        Drug drug,
        Category? categoryInfo,
        List<Core.Domain.Inventory.Entities.ShopInventory> shopInventories,
        Dictionary<string, Core.Domain.Shops.Entities.Shop> shops
    )
    {
        var shopInventorySummaries = shopInventories
            .Where(inv => shops.ContainsKey(inv.ShopId))
            .Select(inv =>
            {
                var shop = shops[inv.ShopId];
                var sellingPrice =
                    inv.ShopPricing?.SellingPrice ?? drug.BasePricing.SuggestedRetailPrice;
                var discount = inv.ShopPricing?.Discount ?? 0;
                var finalPrice = sellingPrice * (1 - discount / 100);
                var nearestExpiry = inv.Batches.Any()
                    ? inv.Batches.Min(b => b.ExpiryDate)
                    : (DateTime?)null;

                return new ShopInventorySummaryDto
                {
                    ShopId = shop.Id,
                    ShopName = shop.ShopName,
                    ShopAddress = $"{shop.Address.Street}, {shop.Address.City}",
                    Quantity = inv.TotalStock,
                    SellingPrice = sellingPrice,
                    DiscountPercentage = discount > 0 ? discount : null,
                    FinalPrice = finalPrice,
                    BatchCount = inv.Batches.Count,
                    NearestExpiryDate = nearestExpiry,
                    IsLowStock = inv.TotalStock <= inv.ReorderPoint,
                    IsAvailable = inv.TotalStock > 0,
                };
            })
            .OrderByDescending(s => s.Quantity)
            .ToList();

        var totalStock = shopInventorySummaries.Sum(s => s.Quantity);
        var shopsWithStock = shopInventorySummaries.Count(s => s.IsAvailable);
        var prices = shopInventorySummaries
            .Where(s => s.IsAvailable)
            .Select(s => s.FinalPrice)
            .ToList();
        var avgPrice = prices.Any() ? prices.Average() : 0;
        var lowestPrice = prices.Any() ? prices.Min() : 0;
        var highestPrice = prices.Any() ? prices.Max() : 0;

        return new DrugDetailDto
        {
            DrugId = drug.DrugId,
            Barcode = drug.Barcode,
            BarcodeType = drug.BarcodeType,
            BrandName = drug.BrandName,
            GenericName = drug.GenericName,
            Manufacturer = drug.Manufacturer,
            OriginCountry = drug.OriginCountry,
            CategoryId = drug.CategoryId,
            Category = categoryInfo?.Name ?? string.Empty,
            CategoryLogoUrl = categoryInfo?.LogoUrl,
            CategoryColorCode = categoryInfo?.ColorCode,
            ImageUrls = drug.ImageUrls,
            Description = drug.Description,
            SideEffects = drug.SideEffects,
            InteractionNotes = drug.InteractionNotes,
            Tags = drug.Tags,
            RelatedDrugs = drug.RelatedDrugs,

            Formulation = new FormulationDto
            {
                Strength = drug.Formulation.Strength,
                Form = drug.Formulation.Form,
                RouteOfAdministration = drug.Formulation.RouteOfAdministration,
                ActiveIngredients = string.Empty,
            },

            Pricing = new PricingDto
            {
                SuggestedRetailPrice = drug.BasePricing.SuggestedRetailPrice,
                WholesalePrice = 0,
                ManufacturerPrice = 0,
            },

            Regulatory = new RegulatoryDto
            {
                IsPrescriptionRequired = drug.Regulatory.IsPrescriptionRequired,
                ControlledSubstanceSchedule = null,
                FdaApprovalNumber = null,
            },

            ShopInventories = shopInventorySummaries,
            TotalQuantityInStock = totalStock,
            TotalShopsWithStock = shopsWithStock,
            AveragePriceAcrossShops = avgPrice,
            LowestPriceAvailable = lowestPrice,
            HighestPriceAvailable = highestPrice,

            CreatedAt = drug.CreatedAt,
            LastUpdated = drug.LastUpdated,
        };
    }

    private static string GenerateDrugId()
    {
        return $"DRG-{Guid.NewGuid():N}".Substring(0, 12).ToUpperInvariant();
    }
}
