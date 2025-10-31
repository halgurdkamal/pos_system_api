using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Services;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Core.Application.Inventory.Queries.GetCashierItems;

public record GetCashierItemsQuery(
    string ShopId,
    string? SearchTerm = null,
    string? Category = null,
    int Page = 1,
    int Limit = 50) : IRequest<PagedResult<ShopPosItemDto>>;

public class GetCashierItemsQueryHandler : IRequestHandler<GetCashierItemsQuery, PagedResult<ShopPosItemDto>>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;
    private readonly IEffectivePackagingService _effectivePackagingService;

    public GetCashierItemsQueryHandler(
        IInventoryRepository inventoryRepository,
        IDrugRepository drugRepository,
        IEffectivePackagingService effectivePackagingService)
    {
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
        _effectivePackagingService = effectivePackagingService;
    }

    public async Task<PagedResult<ShopPosItemDto>> Handle(GetCashierItemsQuery request, CancellationToken cancellationToken)
    {
        // Get all inventory for the shop (no pagination at repo level)
        var allInventory = await _inventoryRepository.GetAllByShopAsync(request.ShopId, cancellationToken);

        // Filter to available items only
        var availableInventory = allInventory.Where(inv => inv.TotalStock > 0).ToList();

        // Get drug IDs
        var drugIds = availableInventory.Select(inv => inv.DrugId).Distinct().ToList();

        // Get all drugs info (using pagination with large limit)
        var allDrugsResult = await _drugRepository.GetAllAsync(1, 10000, cancellationToken);
        var relevantDrugs = allDrugsResult.Data
            .Where(d => drugIds.Contains(d.Id) || drugIds.Contains(d.DrugId))
            .ToList();

        var drugsDict = BuildDrugDictionary(relevantDrugs);

        // Apply search and category filters
        IEnumerable<ShopInventory> query = availableInventory;

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(inv =>
                TryGetDrug(drugsDict, inv.DrugId, out var drug) && (
                    (!string.IsNullOrEmpty(drug.BrandName) && drug.BrandName.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(drug.GenericName) && drug.GenericName.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(drug.Barcode) && drug.Barcode.ToLower().Contains(searchLower)) ||
                    (!string.IsNullOrEmpty(drug.Category?.Name ?? drug.CategoryName) &&
                        (drug.Category?.Name ?? drug.CategoryName).ToLower().Contains(searchLower))
                ));
        }

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(inv =>
                TryGetDrug(drugsDict, inv.DrugId, out var drug) &&
                (
                    (!string.IsNullOrEmpty(drug.CategoryId) && drug.CategoryId.Equals(request.Category, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(drug.Category?.Name ?? drug.CategoryName) &&
                        (drug.Category?.Name ?? drug.CategoryName).Equals(request.Category, StringComparison.OrdinalIgnoreCase))
                ));
        }

        var filteredInventory = query.ToList();
        var totalCount = filteredInventory.Count;

        // Apply pagination
        var pagedInventory = filteredInventory
            .Skip((request.Page - 1) * request.Limit)
            .Take(request.Limit)
            .ToList();

        async Task<(string InventoryId, EffectivePackagingDto? Packaging)> LoadPackagingAsync(ShopInventory inventory)
        {
            try
            {
                var packaging = await _effectivePackagingService.GetEffectivePackagingAsync(
                    inventory.ShopId,
                    inventory.DrugId,
                    cancellationToken);

                return (inventory.Id, packaging);
            }
            catch
            {
                return (inventory.Id, null);
            }
        }

        var packagingResults = await Task.WhenAll(
            pagedInventory.Select(inv => LoadPackagingAsync(inv)));

        var packagingByInventoryId = packagingResults.ToDictionary(
            result => result.InventoryId,
            result => result.Packaging);

        var items = new List<ShopPosItemDto>();

        foreach (var inventory in pagedInventory)
        {
            if (!TryGetDrug(drugsDict, inventory.DrugId, out var drug))
            {
                continue;
            }

            var packaging = packagingByInventoryId.GetValueOrDefault(inventory.Id);
            var primaryImageUrl = drug.ImageUrls?.FirstOrDefault();

            var brandName = !string.IsNullOrWhiteSpace(drug.BrandName)
                ? drug.BrandName
                : "Lipitor";

            var genericName = !string.IsNullOrWhiteSpace(drug.GenericName)
                ? drug.GenericName
                : "Atorvastatin";

            var manufacturer = !string.IsNullOrWhiteSpace(drug.Manufacturer)
                ? drug.Manufacturer
                : "Pfizer Inc.";

            var categoryName = !string.IsNullOrWhiteSpace(drug.Category?.Name ?? drug.CategoryName)
                ? drug.Category?.Name ?? drug.CategoryName
                : "Diabetes";

            var packagingDto = BuildPackagingDto(inventory, drug, packaging);
            var shopPricing = inventory.ShopPricing ?? new ShopPricing();
            var defaultPackagingPrice = packagingDto.PackagingLevels
                .FirstOrDefault(l => l.IsDefaultSellUnit)?.SellingPrice;

            var sellingPrice = defaultPackagingPrice ?? shopPricing.GetFinalPrice();
            if (sellingPrice <= 0 && drug.BasePricing != null)
            {
                sellingPrice = drug.BasePricing.SuggestedRetailPrice;
            }

            var currency = !string.IsNullOrWhiteSpace(shopPricing.Currency)
                ? shopPricing.Currency
                : drug.BasePricing?.Currency ?? "USD";

            var taxRate = shopPricing.TaxRate > 0
                ? shopPricing.TaxRate
                : drug.BasePricing?.SuggestedTaxRate ?? 0;

            items.Add(new ShopPosItemDto
            {
                InventoryId = inventory.Id,
                ShopId = inventory.ShopId,
                DrugId = inventory.DrugId,
                DrugName = BuildDrugDisplayName(drug),
                BrandName = brandName,
                GenericName = genericName,
                Manufacturer = manufacturer,
                Category = categoryName,
                Barcode = drug.Barcode,
                PrimaryImageUrl = primaryImageUrl,
                IsAvailable = inventory.IsAvailable,
                TotalStock = inventory.TotalStock,
                ReorderPoint = inventory.ReorderPoint,
                Packaging = packagingDto,
                ShopPricing = new ShopPosItemPricingDto
                {
                    SellingPrice = sellingPrice,
                    Currency = currency,
                    TaxRate = taxRate
                }
            });
        }

        return new PagedResult<ShopPosItemDto>
        {
            Data = items,
            Total = totalCount,
            Page = request.Page,
            Limit = request.Limit
        };
    }

    private static Dictionary<string, Drug> BuildDrugDictionary(IEnumerable<Drug> drugs)
    {
        var dictionary = new Dictionary<string, Drug>(StringComparer.OrdinalIgnoreCase);

        foreach (var drug in drugs)
        {
            if (!string.IsNullOrWhiteSpace(drug.Id))
            {
                dictionary[drug.Id] = drug;
            }

            if (!string.IsNullOrWhiteSpace(drug.DrugId))
            {
                dictionary[drug.DrugId] = drug;
            }
        }

        return dictionary;
    }

    private static bool TryGetDrug(Dictionary<string, Drug> drugs, string key, out Drug drug)
    {
        return drugs.TryGetValue(key, out drug);
    }

    private static ShopPosItemPackagingDto BuildPackagingDto(
        ShopInventory inventory,
        Drug drug,
        EffectivePackagingDto? packaging)
    {
        var packagingLevels = new List<ShopPosItemPackagingLevelDto>();

        if (packaging?.PackagingLevels != null && packaging.PackagingLevels.Count > 0)
        {
            foreach (var level in packaging.PackagingLevels.OrderBy(l => l.Sequence))
            {
                packagingLevels.Add(new ShopPosItemPackagingLevelDto
                {
                    UnitName = level.UnitName,
                    IsDefaultSellUnit = level.IsDefaultSellUnit,
                    IsSellable = level.IsSellable,
                    EffectiveBaseUnitQuantity = level.EffectiveBaseUnitQuantity,
                    SellingPrice = level.SellingPrice,
                    MinimumSaleQuantity = level.MinimumSaleQuantity
                });
            }

            var effectiveDefault = packaging.PackagingLevels
                .FirstOrDefault(l => l.IsDefaultSellUnit)?.UnitName
                ?? packaging.PackagingLevels.FirstOrDefault()?.UnitName
                ?? inventory.ShopSpecificSellUnit
                ?? drug.PackagingInfo?.GetDefaultSellUnit()?.UnitName
                ?? string.Empty;

            return new ShopPosItemPackagingDto
            {
                DefaultSellUnit = effectiveDefault,
                PackagingLevels = packagingLevels
            };
        }

        var fallbackDefault = inventory.ShopSpecificSellUnit
            ?? drug.PackagingInfo?.GetDefaultSellUnit()?.UnitName
            ?? drug.PackagingInfo?.PackagingLevels.FirstOrDefault()?.UnitName
            ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(fallbackDefault))
        {
            packagingLevels.Add(new ShopPosItemPackagingLevelDto
            {
                UnitName = fallbackDefault,
                IsDefaultSellUnit = true,
                IsSellable = true,
                EffectiveBaseUnitQuantity = 1,
                SellingPrice = inventory.ShopPricing?.GetFinalPrice()
                    ?? drug.BasePricing?.SuggestedRetailPrice
                    ?? 0,
                MinimumSaleQuantity = inventory.MinimumSaleQuantity
            });
        }

        return new ShopPosItemPackagingDto
        {
            DefaultSellUnit = fallbackDefault,
            PackagingLevels = packagingLevels
        };
    }

    private static string BuildDrugDisplayName(Drug drug)
    {
        var name = !string.IsNullOrWhiteSpace(drug.BrandName)
            ? drug.BrandName
            : drug.GenericName;

        var strength = drug.Formulation?.Strength?.Trim();
        if (!string.IsNullOrWhiteSpace(strength))
        {
            name = string.IsNullOrWhiteSpace(name)
                ? strength
                : $"{name} {strength}";
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            name = !string.IsNullOrWhiteSpace(drug.DrugId) ? drug.DrugId : drug.Id;
        }

        return name;
    }
}
