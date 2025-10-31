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

public record CreatePackagingOverrideCommand(
    string ShopId,
    string DrugId,
    PackagingOverrideInputDto Payload) : IRequest<EffectivePackagingDto>;

public class CreatePackagingOverrideCommandHandler : IRequestHandler<CreatePackagingOverrideCommand, EffectivePackagingDto>
{
    private readonly IShopPackagingOverrideRepository _overrideRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IDrugRepository _drugRepository;
    private readonly IEffectivePackagingService _packagingService;
    private readonly ILogger<CreatePackagingOverrideCommandHandler> _logger;

    public CreatePackagingOverrideCommandHandler(
        IShopPackagingOverrideRepository overrideRepository,
        IInventoryRepository inventoryRepository,
        IDrugRepository drugRepository,
        IEffectivePackagingService packagingService,
        ILogger<CreatePackagingOverrideCommandHandler> logger)
    {
        _overrideRepository = overrideRepository;
        _inventoryRepository = inventoryRepository;
        _drugRepository = drugRepository;
        _packagingService = packagingService;
        _logger = logger;
    }

    public async Task<EffectivePackagingDto> Handle(CreatePackagingOverrideCommand request, CancellationToken cancellationToken)
    {
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(request.ShopId, request.DrugId, cancellationToken)
                        ?? throw new KeyNotFoundException($"Inventory not found for shop {request.ShopId} and drug {request.DrugId}.");

        var drug = await _drugRepository.GetByIdAsync(request.DrugId, cancellationToken)
                   ?? throw new KeyNotFoundException($"Drug {request.DrugId} was not found.");

        var existingOverrides = (await _overrideRepository
                .GetByShopAndDrugAsync(request.ShopId, request.DrugId, cancellationToken))
            .ToList();

        var payload = request.Payload ?? throw new ArgumentException("Payload is required.");

        var isGlobalOverride = !string.IsNullOrWhiteSpace(payload.PackagingLevelId);
        var globalLevel = isGlobalOverride
            ? ResolveGlobalLevel(drug.PackagingInfo, payload.PackagingLevelId!)
            : null;

        ValidatePayload(payload, globalLevel, existingOverrides);

        if (!string.IsNullOrWhiteSpace(payload.ParentPackagingLevelId))
        {
            ResolveGlobalLevel(drug.PackagingInfo, payload.ParentPackagingLevelId);
        }

        if (isGlobalOverride && existingOverrides.Any(o =>
                o.PackagingLevelId != null &&
                o.PackagingLevelId.Equals(payload.PackagingLevelId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Override already exists for packaging level {payload.PackagingLevelId}. Use update endpoint.");
        }

        var overrideEntity = new ShopPackagingOverride
        {
            ShopId = request.ShopId,
            DrugId = request.DrugId,
            PackagingLevelId = payload.PackagingLevelId,
            ParentPackagingLevelId = payload.ParentPackagingLevelId,
            ParentOverrideId = payload.ParentOverrideId,
            CustomUnitName = payload.CustomUnitName?.Trim(),
            OverrideQuantityPerParent = payload.OverrideQuantityPerParent,
            SellingPrice = payload.SellingPrice,
            IsSellable = payload.IsSellable,
            IsDefaultSellUnit = payload.IsDefaultSellUnit,
            MinimumSaleQuantity = payload.MinimumSaleQuantity,
            CustomLevelOrder = payload.CustomLevelOrder,
            CreatedBy = "system"
        };

        if (payload.IsDefaultSellUnit == true)
        {
            await ClearDefaultOverridesAsync(existingOverrides, cancellationToken);
            inventory.ShopSpecificSellUnit = overrideEntity.CustomUnitName ?? globalLevel?.UnitName ?? inventory.ShopSpecificSellUnit;
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);
        }

        await _overrideRepository.AddAsync(overrideEntity, cancellationToken);

        _logger.LogInformation("Created packaging override {OverrideId} for shop {ShopId} drug {DrugId}.",
            overrideEntity.Id, request.ShopId, request.DrugId);

        return await _packagingService.GetEffectivePackagingAsync(request.ShopId, request.DrugId, cancellationToken);
    }

    private static PackagingLevel? ResolveGlobalLevel(PackagingInfo? packagingInfo, string packagingLevelId)
    {
        var level = packagingInfo?.GetLevelById(packagingLevelId);
        if (level == null)
        {
            throw new KeyNotFoundException($"Packaging level {packagingLevelId} not found in global catalog.");
        }

        return level;
    }

    private static void ValidatePayload(
        PackagingOverrideInputDto payload,
        PackagingLevel? globalLevel,
        IReadOnlyCollection<ShopPackagingOverride> existingOverrides)
    {
        if (string.IsNullOrWhiteSpace(payload.PackagingLevelId))
        {
            if (string.IsNullOrWhiteSpace(payload.CustomUnitName))
            {
                throw new ArgumentException("CustomUnitName is required when creating a custom packaging level.");
            }

            if (!payload.OverrideQuantityPerParent.HasValue || payload.OverrideQuantityPerParent <= 0)
            {
                throw new ArgumentException("OverrideQuantityPerParent must be greater than zero for custom packaging levels.");
            }

            if (!string.IsNullOrWhiteSpace(payload.ParentOverrideId) &&
                !string.IsNullOrWhiteSpace(payload.ParentPackagingLevelId))
            {
                throw new ArgumentException("Specify either ParentOverrideId or ParentPackagingLevelId, not both.");
            }

            if (existingOverrides.Any(o =>
                    o.PackagingLevelId == null &&
                    o.CustomUnitName != null &&
                    o.CustomUnitName.Equals(payload.CustomUnitName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"A custom packaging level named '{payload.CustomUnitName}' already exists.");
            }

            if (!string.IsNullOrWhiteSpace(payload.ParentOverrideId) &&
                existingOverrides.All(o => !o.Id.Equals(payload.ParentOverrideId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Parent override {payload.ParentOverrideId} was not found.");
            }
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(payload.ParentOverrideId))
            {
                throw new ArgumentException("Global overrides cannot reference custom parents.");
            }

            if (globalLevel == null)
            {
                throw new ArgumentException("Global packaging level reference is invalid.");
            }

            if (payload.OverrideQuantityPerParent.HasValue && payload.OverrideQuantityPerParent <= 0)
            {
                throw new ArgumentException("OverrideQuantityPerParent must be greater than zero when specified.");
            }
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
