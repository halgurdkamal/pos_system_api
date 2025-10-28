using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Application.Inventory.Services;
using pos_system_api.Core.Domain.Drugs.ValueObjects;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Inventory.Commands.PackagingOverrides;

public record UpdatePackagingLevelCommand(
    string ShopId,
    string DrugId,
    string TargetLevelId,
    PackagingOverrideInputDto Payload) : IRequest<EffectivePackagingDto>;

public class UpdatePackagingLevelCommandHandler : IRequestHandler<UpdatePackagingLevelCommand, EffectivePackagingDto>
{
    private readonly IShopPackagingOverrideRepository _overrideRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;
    private readonly IEffectivePackagingService _packagingService;
    private readonly ILogger<UpdatePackagingLevelCommandHandler> _logger;

    public UpdatePackagingLevelCommandHandler(
        IShopPackagingOverrideRepository overrideRepository,
        IInventoryRepository inventoryRepository,
        IDrugRepository drugRepository,
        IEffectivePackagingService packagingService,
        ILogger<UpdatePackagingLevelCommandHandler> logger)
    {
        _overrideRepository = overrideRepository;
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
        _packagingService = packagingService;
        _logger = logger;
    }

    public async Task<EffectivePackagingDto> Handle(UpdatePackagingLevelCommand request, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(request.ShopId, request.DrugId, cancellationToken)
                        ?? throw new KeyNotFoundException($"Inventory not found for shop {request.ShopId} and drug {request.DrugId}.");

        var drug = await _drugRepository.GetByIdAsync(request.DrugId, cancellationToken)
                   ?? throw new KeyNotFoundException($"Drug {request.DrugId} was not found.");

        var payload = request.Payload ?? throw new ArgumentException("Payload is required.");

        var overrides = (await _overrideRepository.GetByShopAndDrugAsync(request.ShopId, request.DrugId, cancellationToken))
            .ToList();

        var existingOverride = overrides.FirstOrDefault(o =>
            o.Id.Equals(request.TargetLevelId, StringComparison.OrdinalIgnoreCase));

        PackagingLevel? globalLevel = null;
        var isGlobalReference = existingOverride == null;
        if (isGlobalReference)
        {
            globalLevel = ResolveGlobalLevel(drug.PackagingInfo, request.TargetLevelId);
            existingOverride = overrides.FirstOrDefault(o =>
                o.PackagingLevelId != null &&
                o.PackagingLevelId.Equals(request.TargetLevelId, StringComparison.OrdinalIgnoreCase));
        }

        var isNewOverride = false;
        if (existingOverride == null)
        {
            existingOverride = new ShopPackagingOverride
            {
                ShopId = request.ShopId,
                DrugId = request.DrugId,
                PackagingLevelId = isGlobalReference ? request.TargetLevelId : null,
                CreatedBy = "system"
            };
            isNewOverride = true;
        }

        ValidatePayload(payload, globalLevel, overrides, existingOverride);

        if (!string.IsNullOrWhiteSpace(payload.ParentPackagingLevelId))
        {
            ResolveGlobalLevel(drug.PackagingInfo, payload.ParentPackagingLevelId);
        }

        if (payload.ParentPackagingLevelId != null || isNewOverride)
        {
            existingOverride.ParentPackagingLevelId = payload.ParentPackagingLevelId;
        }

        if (payload.ParentOverrideId != null || isNewOverride)
        {
            existingOverride.ParentOverrideId = payload.ParentOverrideId;
        }

        if (payload.CustomUnitName != null || isNewOverride)
        {
            existingOverride.CustomUnitName = payload.CustomUnitName?.Trim();
        }

        if (payload.OverrideQuantityPerParent.HasValue || isNewOverride)
        {
            existingOverride.OverrideQuantityPerParent = payload.OverrideQuantityPerParent;
        }

        if (payload.SellingPrice.HasValue || isNewOverride)
        {
            existingOverride.SellingPrice = payload.SellingPrice;
        }

        existingOverride.IsSellable = payload.IsSellable ?? existingOverride.IsSellable;
        existingOverride.IsDefaultSellUnit = payload.IsDefaultSellUnit ?? existingOverride.IsDefaultSellUnit;

        if (payload.MinimumSaleQuantity.HasValue || isNewOverride)
        {
            existingOverride.MinimumSaleQuantity = payload.MinimumSaleQuantity;
        }

        if (payload.CustomLevelOrder.HasValue || isNewOverride)
        {
            existingOverride.CustomLevelOrder = payload.CustomLevelOrder;
        }
        existingOverride.UpdatedBy = "system";

        if (isNewOverride)
        {
            await _overrideRepository.AddAsync(existingOverride, cancellationToken);
        }
        else
        {
            await _overrideRepository.UpdateAsync(existingOverride, cancellationToken);
        }

        if (existingOverride.IsDefaultSellUnit == true)
        {
            await ClearDefaultOverridesAsync(overrides.Where(o => !ReferenceEquals(o, existingOverride)), cancellationToken);
            inventory.ShopSpecificSellUnit = existingOverride.CustomUnitName ?? globalLevel?.UnitName ?? inventory.ShopSpecificSellUnit;
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);
        }
        else if (payload.IsDefaultSellUnit == false && inventory.ShopSpecificSellUnit != null &&
                 string.Equals(inventory.ShopSpecificSellUnit, existingOverride.CustomUnitName ?? globalLevel?.UnitName, StringComparison.OrdinalIgnoreCase))
        {
            inventory.ShopSpecificSellUnit = null;
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);
        }

        _logger.LogInformation("Updated packaging override {Target} for shop {ShopId} drug {DrugId}.",
            request.TargetLevelId, request.ShopId, request.DrugId);

        return await _packagingService.GetEffectivePackagingAsync(request.ShopId, request.DrugId, cancellationToken);
    }

    private static PackagingLevel ResolveGlobalLevel(PackagingInfo? packagingInfo, string packagingLevelId)
    {
        var level = packagingInfo?.GetLevelById(packagingLevelId);
        if (level == null)
        {
            throw new KeyNotFoundException($"Packaging level {packagingLevelId} not found.");
        }

        return level;
    }

    private static void ValidatePayload(
        PackagingOverrideInputDto payload,
        PackagingLevel? globalLevel,
        IReadOnlyCollection<ShopPackagingOverride> existingOverrides,
        ShopPackagingOverride currentOverride)
    {
        if (globalLevel == null && string.IsNullOrWhiteSpace(currentOverride.PackagingLevelId))
        {
            if (string.IsNullOrWhiteSpace(payload.CustomUnitName) && string.IsNullOrWhiteSpace(currentOverride.CustomUnitName))
            {
                throw new ArgumentException("CustomUnitName is required for custom levels.");
            }

            if (payload.OverrideQuantityPerParent.HasValue && payload.OverrideQuantityPerParent <= 0)
            {
                throw new ArgumentException("OverrideQuantityPerParent must be greater than zero.");
            }
        }

        if (!string.IsNullOrWhiteSpace(payload.ParentOverrideId) &&
            existingOverrides.All(o => !o.Id.Equals(payload.ParentOverrideId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Parent override {payload.ParentOverrideId} was not found.");
        }

        if (!string.IsNullOrWhiteSpace(payload.ParentOverrideId) &&
            !string.IsNullOrWhiteSpace(payload.ParentPackagingLevelId))
        {
            throw new ArgumentException("Specify either ParentOverrideId or ParentPackagingLevelId, not both.");
        }

        if (globalLevel != null &&
            payload.OverrideQuantityPerParent.HasValue &&
            payload.OverrideQuantityPerParent <= 0)
        {
            throw new ArgumentException("OverrideQuantityPerParent must be greater than zero when specified.");
        }
    }

    private async Task ClearDefaultOverridesAsync(
        IEnumerable<ShopPackagingOverride> overrides,
        CancellationToken cancellationToken)
    {
        foreach (var existing in overrides.Where(o => o.IsDefaultSellUnit == true))
        {
            existing.IsDefaultSellUnit = false;
            await _overrideRepository.UpdateAsync(existing, cancellationToken);
        }
    }
}
