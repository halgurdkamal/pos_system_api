using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;

namespace pos_system_api.Core.Application.Drugs.Commands.CreateDrug;

public class CreateDrugCommandHandler : IRequestHandler<CreateDrugCommand, DrugDto>
{
    private readonly IDrugRepository _drugRepository;
    private readonly ILogger<CreateDrugCommandHandler> _logger;
    private readonly ICategoryRepository _categoryRepository;

    public CreateDrugCommandHandler(
        IDrugRepository drugRepository,
        ICategoryRepository categoryRepository,
        ILogger<CreateDrugCommandHandler> logger)
    {
        _drugRepository = drugRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<DrugDto> Handle(CreateDrugCommand request, CancellationToken cancellationToken)
    {
        if (request.Payload == null)
        {
            throw new ArgumentNullException(nameof(request.Payload));
        }

        var dto = request.Payload;

        if (string.IsNullOrWhiteSpace(dto.BrandName))
            throw new ArgumentException("BrandName is required.", nameof(dto.BrandName));

        if (string.IsNullOrWhiteSpace(dto.GenericName))
            throw new ArgumentException("GenericName is required.", nameof(dto.GenericName));

        if (string.IsNullOrWhiteSpace(dto.CategoryId))
            throw new ArgumentException("CategoryId is required.", nameof(dto.CategoryId));

        if (dto.PackagingInfo == null)
            throw new ArgumentException("PackagingInfo is required.", nameof(dto.PackagingInfo));

        if (dto.PackagingInfo.PackagingLevels == null || dto.PackagingInfo.PackagingLevels.Count == 0)
            throw new ArgumentException("PackagingLevels must contain at least one level.", nameof(dto.PackagingInfo.PackagingLevels));

        var desiredDrugId = dto.DrugId?.Trim();
        if (!string.IsNullOrWhiteSpace(desiredDrugId))
        {
            var existingDrug = await _drugRepository.GetByIdAsync(desiredDrugId, cancellationToken);
            if (existingDrug != null)
            {
                throw new InvalidOperationException($"Drug with ID '{desiredDrugId}' already exists.");
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var existingBarcode = await _drugRepository.GetByBarcodeAsync(dto.Barcode.Trim(), cancellationToken);
            if (existingBarcode != null)
            {
                throw new InvalidOperationException($"Barcode '{dto.Barcode}' is already assigned to another drug.");
            }
        }

        var category = await _categoryRepository.GetByIdAsync(dto.CategoryId.Trim(), cancellationToken);
        if (category == null)
        {
            throw new InvalidOperationException($"Category '{dto.CategoryId}' does not exist.");
        }

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
            ImageUrls = dto.ImageUrls?.Where(url => !string.IsNullOrWhiteSpace(url)).Select(url => url.Trim()).ToList() ?? new List<string>(),
            Description = dto.Description ?? string.Empty,
            SideEffects = dto.SideEffects ?? new List<string>(),
            InteractionNotes = dto.InteractionNotes ?? new List<string>(),
            Tags = dto.Tags ?? new List<string>(),
            RelatedDrugs = dto.RelatedDrugs ?? new List<string>(),
            Formulation = MapFormulation(dto.Formulation),
            BasePricing = MapBasePricing(dto.BasePricing),
            Regulatory = MapRegulatory(dto.Regulatory)
        };

        drug.PackagingInfo = BuildPackagingInfo(dto.PackagingInfo);

        var created = await _drugRepository.CreateAsync(drug, cancellationToken);
        created.Category = category;

        _logger.LogInformation("Created drug {DrugId} with barcode {Barcode}.", created.DrugId, created.Barcode);

        return MapToDto(created);
    }

    private static string GenerateDrugId()
    {
        return $"DRG-{Guid.NewGuid():N}".Substring(0, 12).ToUpperInvariant();
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
            RouteOfAdministration = source.RouteOfAdministration?.Trim() ?? string.Empty
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
            LastPriceUpdate = DateTime.UtcNow
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
            ControlSchedule = source.ControlSchedule?.Trim() ?? string.Empty
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
                packagingLevelId: string.IsNullOrWhiteSpace(level.PackagingLevelId) ? null : level.PackagingLevelId.Trim(),
                levelNumber: level.LevelNumber,
                unitName: level.UnitName?.Trim() ?? string.Empty,
                baseUnitQuantity: level.BaseUnitQuantity,
                isSellable: level.IsSellable,
                isDefault: level.IsDefault,
                isBreakable: level.IsBreakable,
                barcode: string.IsNullOrWhiteSpace(level.Barcode) ? null : level.Barcode.Trim(),
                minimumSaleQuantity: level.MinimumSaleQuantity,
                parentPackagingLevelId: string.IsNullOrWhiteSpace(level.ParentPackagingLevelId) ? null : level.ParentPackagingLevelId.Trim(),
                quantityPerParent: level.QuantityPerParent);

            packagingInfo.AddPackagingLevel(packagingLevel);
        }

        var validation = packagingInfo.Validate();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Invalid packaging configuration: {string.Join("; ", validation.Errors)}");
        }

        return packagingInfo;
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
            UpdatedBy = drug.UpdatedBy
        };
    }
}
