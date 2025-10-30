using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using pos_system_api.Core.Domain.Drugs.ValueObjects;

namespace pos_system_api.Core.Application.Drugs.DTOs;

/// <summary>
/// Request payload for creating a drug in the catalog.
/// </summary>
public class CreateDrugDto
{
    public string? DrugId { get; set; }
    public string? Barcode { get; set; }
    public string? BarcodeType { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string OriginCountry { get; set; } = string.Empty;
    public string CategoryId { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public List<string> SideEffects { get; set; } = new();
    public List<string> InteractionNotes { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> RelatedDrugs { get; set; } = new();
    public CreateFormulationDto? Formulation { get; set; }
    public CreateBasePricingDto? BasePricing { get; set; }
    public CreateRegulatoryDto? Regulatory { get; set; }
    public CreatePackagingInfoDto PackagingInfo { get; set; } = new();
}

public class CreateFormulationDto
{
    public string Form { get; set; } = string.Empty;
    public string Strength { get; set; } = string.Empty;
    public string RouteOfAdministration { get; set; } = string.Empty;
}

public class CreateBasePricingDto
{
    public decimal SuggestedRetailPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal SuggestedTaxRate { get; set; }
}

public class CreateRegulatoryDto
{
    public bool IsPrescriptionRequired { get; set; }
    public bool IsHighRisk { get; set; }
    public string DrugAuthorityNumber { get; set; } = string.Empty;
    public DateTime ApprovalDate { get; set; }
    public string ControlSchedule { get; set; } = string.Empty;
}

public class CreatePackagingInfoDto
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public UnitType UnitType { get; set; }
    public string BaseUnit { get; set; } = string.Empty;
    public string BaseUnitDisplayName { get; set; } = string.Empty;
    public bool IsSubdivisible { get; set; } = true;
    public List<CreatePackagingLevelDto> PackagingLevels { get; set; } = new();
}

public class CreatePackagingLevelDto
{
    public string? PackagingLevelId { get; set; }
    public int LevelNumber { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string? ParentPackagingLevelId { get; set; }
    public decimal BaseUnitQuantity { get; set; }
    public decimal? QuantityPerParent { get; set; }
    public bool IsSellable { get; set; } = true;
    public bool IsDefault { get; set; }
    public bool IsBreakable { get; set; } = true;
    public string? Barcode { get; set; }
    public decimal? MinimumSaleQuantity { get; set; }
}
