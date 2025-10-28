using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Drugs.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Inventory.Services;

public class EffectivePackagingService : IEffectivePackagingService
{
    private readonly IDrugRepository _drugRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IShopPackagingOverrideRepository _overrideRepository;
    private readonly ILogger<EffectivePackagingService> _logger;

    public EffectivePackagingService(
        IDrugRepository drugRepository,
        IInventoryRepository inventoryRepository,
        IShopPackagingOverrideRepository overrideRepository,
        ILogger<EffectivePackagingService> logger)
    {
        _drugRepository = drugRepository;
        _inventoryRepository = inventoryRepository;
        _overrideRepository = overrideRepository;
        _logger = logger;
    }

    public async Task<EffectivePackagingDto> GetEffectivePackagingAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default)
    {
        var drug = await _drugRepository.GetByIdAsync(drugId, cancellationToken)
                   ?? throw new KeyNotFoundException($"Drug {drugId} was not found.");

        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(shopId, drugId, cancellationToken);
        var overrides = await _overrideRepository.GetByShopAndDrugAsync(shopId, drugId, cancellationToken);

        _logger.LogDebug("Merging packaging for shop {ShopId} and drug {DrugId}. Found {GlobalCount} global levels and {OverrideCount} overrides.",
            shopId,
            drugId,
            drug.PackagingInfo?.PackagingLevels?.Count ?? 0,
            overrides.Count);

        var dto = new EffectivePackagingDto
        {
            ShopId = shopId,
            DrugId = drugId
        };

        var globalLevels = (drug.PackagingInfo?.PackagingLevels ?? new List<PackagingLevel>())
            .OrderBy(l => l.LevelNumber)
            .ToList();

        var overrideLookup = overrides
            .Where(o => !string.IsNullOrWhiteSpace(o.PackagingLevelId))
            .ToDictionary(o => o.PackagingLevelId!, StringComparer.OrdinalIgnoreCase);

        var effectiveBaseUnitsByLevel = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var effectiveQuantityByLevel = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var level in globalLevels)
        {
            overrideLookup.TryGetValue(level.PackagingLevelId, out var levelOverride);

            var parentBaseUnits = ResolveParentBaseUnits(level, effectiveBaseUnitsByLevel, overrideLookup, globalLevels);
            var effectiveQuantityPerParent = level.LevelNumber == 1
                ? 1m
                : levelOverride?.OverrideQuantityPerParent ?? level.QuantityPerParent;

            var effectiveBaseUnits = level.LevelNumber == 1
                ? 1m
                : parentBaseUnits * effectiveQuantityPerParent;

            effectiveBaseUnitsByLevel[level.PackagingLevelId] = effectiveBaseUnits;
            effectiveQuantityByLevel[level.PackagingLevelId] = effectiveQuantityPerParent;

            var sellingPrice = ResolveSellingPrice(level.UnitName, levelOverride, inventory, drug);
            var isSellable = levelOverride?.IsSellable ?? level.IsSellable;
            var minimumSaleQuantity = levelOverride?.MinimumSaleQuantity
                                      ?? level.MinimumSaleQuantity
                                      ?? inventory?.MinimumSaleQuantity;

            var effectiveLevel = new EffectivePackagingLevelDto
            {
                LevelId = level.PackagingLevelId,
                OverrideId = levelOverride?.Id,
                UnitName = level.UnitName,
                IsGlobal = true,
                GlobalIsSellable = level.IsSellable,
                IsSellable = isSellable,
                IsDefaultSellUnit = levelOverride?.IsDefaultSellUnit ?? false,
                GlobalBaseUnitQuantity = level.BaseUnitQuantity,
                EffectiveBaseUnitQuantity = effectiveBaseUnits,
                GlobalQuantityPerParent = level.QuantityPerParent,
                EffectiveQuantityPerParent = effectiveQuantityPerParent,
                OverrideQuantityPerParent = levelOverride?.OverrideQuantityPerParent,
                SellingPrice = sellingPrice,
                MinimumSaleQuantity = minimumSaleQuantity,
                ParentLevelId = level.ParentPackagingLevelId,
                ParentOverrideId = levelOverride?.ParentOverrideId,
                Sequence = level.LevelNumber
            };

            dto.PackagingLevels.Add(effectiveLevel);
        }

        var customOverrides = overrides
            .Where(o => string.IsNullOrWhiteSpace(o.PackagingLevelId))
            .ToDictionary(o => o.Id, StringComparer.OrdinalIgnoreCase);

        var customBaseUnits = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var customOverride in customOverrides.Values)
        {
            var baseUnits = ResolveCustomBaseUnits(
                customOverride,
                effectiveBaseUnitsByLevel,
                customOverrides,
                customBaseUnits);

            var parentEffectiveQuantity = customOverride.ParentOverrideId != null
                ? customBaseUnits.TryGetValue(customOverride.ParentOverrideId, out var parentBase) ? parentBase : 1m
                : customOverride.ParentPackagingLevelId != null && effectiveBaseUnitsByLevel.TryGetValue(customOverride.ParentPackagingLevelId, out var parentGlobalBase)
                    ? parentGlobalBase
                    : 1m;

            var effectiveQuantityPerParent = customOverride.OverrideQuantityPerParent ?? (parentEffectiveQuantity == 0 ? 0 : baseUnits / parentEffectiveQuantity);
            var sellingPrice = customOverride.SellingPrice
                               ?? ResolveSellingPrice(customOverride.CustomUnitName ?? string.Empty, customOverride, inventory, drug);

            var effectiveLevel = new EffectivePackagingLevelDto
            {
                LevelId = null,
                OverrideId = customOverride.Id,
                UnitName = customOverride.CustomUnitName ?? string.Empty,
                IsGlobal = false,
                GlobalIsSellable = null,
                IsSellable = customOverride.IsSellable ?? true,
                IsDefaultSellUnit = customOverride.IsDefaultSellUnit ?? false,
                GlobalBaseUnitQuantity = 0,
                EffectiveBaseUnitQuantity = baseUnits,
                GlobalQuantityPerParent = 0,
                EffectiveQuantityPerParent = effectiveQuantityPerParent,
                OverrideQuantityPerParent = customOverride.OverrideQuantityPerParent,
                SellingPrice = sellingPrice,
                MinimumSaleQuantity = customOverride.MinimumSaleQuantity ?? inventory?.MinimumSaleQuantity,
                ParentLevelId = customOverride.ParentPackagingLevelId,
                ParentOverrideId = customOverride.ParentOverrideId,
                Sequence = customOverride.CustomLevelOrder ?? int.MaxValue
            };

            dto.PackagingLevels.Add(effectiveLevel);
        }

        HarmonizeDefaultSellUnit(dto, globalLevels, inventory);
        dto.PackagingLevels = dto.PackagingLevels
            .OrderBy(l => l.IsGlobal ? 0 : 1)
            .ThenBy(l => l.Sequence)
            .ThenBy(l => l.UnitName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return dto;
    }

    private static decimal ResolveParentBaseUnits(
        PackagingLevel level,
        IDictionary<string, decimal> effectiveBaseUnitsByLevel,
        IDictionary<string, ShopPackagingOverride> overrideLookup,
        IList<PackagingLevel> globalLevels)
    {
        if (level.LevelNumber == 1)
        {
            return 1m;
        }

        if (!string.IsNullOrWhiteSpace(level.ParentPackagingLevelId) &&
            effectiveBaseUnitsByLevel.TryGetValue(level.ParentPackagingLevelId, out var parentBaseUnits))
        {
            return parentBaseUnits;
        }

        var parentLevel = globalLevels.FirstOrDefault(l =>
            l.LevelNumber == level.LevelNumber - 1);

        if (parentLevel != null &&
            effectiveBaseUnitsByLevel.TryGetValue(parentLevel.PackagingLevelId, out var inferredBaseUnits))
        {
            return inferredBaseUnits;
        }

        return 1m;
    }

    private decimal ResolveCustomBaseUnits(
        ShopPackagingOverride customOverride,
        IDictionary<string, decimal> effectiveGlobalBaseUnits,
        IDictionary<string, ShopPackagingOverride> allCustomOverrides,
        IDictionary<string, decimal> memo)
    {
        if (memo.TryGetValue(customOverride.Id, out var cached))
        {
            return cached;
        }

        decimal parentBaseUnits = 1m;

        if (!string.IsNullOrWhiteSpace(customOverride.ParentOverrideId))
        {
            if (!allCustomOverrides.TryGetValue(customOverride.ParentOverrideId, out var parentOverride))
            {
                throw new InvalidOperationException($"Parent override {customOverride.ParentOverrideId} not found for override {customOverride.Id}.");
            }

            parentBaseUnits = ResolveCustomBaseUnits(parentOverride, effectiveGlobalBaseUnits, allCustomOverrides, memo);
        }
        else if (!string.IsNullOrWhiteSpace(customOverride.ParentPackagingLevelId))
        {
            if (!effectiveGlobalBaseUnits.TryGetValue(customOverride.ParentPackagingLevelId, out parentBaseUnits))
            {
                throw new InvalidOperationException($"Parent packaging level {customOverride.ParentPackagingLevelId} not resolved for override {customOverride.Id}.");
            }
        }

        var quantityPerParent = customOverride.OverrideQuantityPerParent ?? 0;
        if (quantityPerParent <= 0)
        {
            throw new InvalidOperationException($"Override {customOverride.Id} must specify OverrideQuantityPerParent greater than zero.");
        }

        var baseUnits = parentBaseUnits * quantityPerParent;
        memo[customOverride.Id] = baseUnits;
        return baseUnits;
    }

    private static decimal? ResolveSellingPrice(
        string unitKey,
        ShopPackagingOverride? overrideEntity,
        ShopInventory? inventory,
        Drug drug)
    {
        if (overrideEntity?.SellingPrice.HasValue == true)
        {
            return overrideEntity.SellingPrice;
        }

        if (!string.IsNullOrWhiteSpace(unitKey) &&
            inventory?.ShopPricing?.PackagingLevelPrices != null &&
            inventory.ShopPricing.PackagingLevelPrices.TryGetValue(unitKey, out var levelPrice))
        {
            return levelPrice;
        }

        if (inventory?.ShopPricing != null && inventory.ShopPricing.SellingPrice > 0)
        {
            return inventory.ShopPricing.SellingPrice;
        }

        return drug.BasePricing?.SuggestedRetailPrice;
    }

    private static void HarmonizeDefaultSellUnit(
        EffectivePackagingDto dto,
        IList<PackagingLevel> globalLevels,
        ShopInventory? inventory)
    {
        var explicitDefault = dto.PackagingLevels.FirstOrDefault(l => l.IsDefaultSellUnit);
        if (explicitDefault != null)
        {
            foreach (var level in dto.PackagingLevels.Where(l => !ReferenceEquals(l, explicitDefault)))
            {
                level.IsDefaultSellUnit = false;
            }
            return;
        }

        var preferredUnit = inventory?.ShopSpecificSellUnit;
        if (!string.IsNullOrWhiteSpace(preferredUnit))
        {
            var preferred = dto.PackagingLevels.FirstOrDefault(l =>
                l.UnitName.Equals(preferredUnit, StringComparison.OrdinalIgnoreCase));

            if (preferred != null)
            {
                preferred.IsDefaultSellUnit = true;
                foreach (var level in dto.PackagingLevels.Where(l => !ReferenceEquals(l, preferred)))
                {
                    level.IsDefaultSellUnit = false;
                }
                return;
            }
        }

        var globalDefaultLevelId = globalLevels.FirstOrDefault(l => l.IsDefault)?.PackagingLevelId;
        if (globalDefaultLevelId != null)
        {
            var globalDefault = dto.PackagingLevels.FirstOrDefault(l =>
                l.LevelId != null &&
                l.LevelId.Equals(globalDefaultLevelId, StringComparison.OrdinalIgnoreCase));

            if (globalDefault != null)
            {
                globalDefault.IsDefaultSellUnit = true;
                foreach (var level in dto.PackagingLevels.Where(l => !ReferenceEquals(l, globalDefault)))
                {
                    level.IsDefaultSellUnit = false;
                }
                return;
            }
        }

        var firstSellable = dto.PackagingLevels.FirstOrDefault(l => l.IsSellable);
        if (firstSellable != null)
        {
            firstSellable.IsDefaultSellUnit = true;
        }
    }
}
