using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;
using Microsoft.Extensions.Logging;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdatePackagingPricing;

public class UpdatePackagingPricingCommandHandler : IRequestHandler<UpdatePackagingPricingCommand, InventoryDto>
{
    private readonly IInventoryRepository _repository;
    private readonly ILogger<UpdatePackagingPricingCommandHandler> _logger;

    public UpdatePackagingPricingCommandHandler(
        IInventoryRepository repository,
        ILogger<UpdatePackagingPricingCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<InventoryDto> Handle(UpdatePackagingPricingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating packaging-level pricing for drug {DrugId} in shop {ShopId}",
            request.DrugId, request.ShopId);

        var inventory = await _repository.GetByShopAndDrugAsync(request.ShopId, request.DrugId, cancellationToken);
        if (inventory == null)
        {
            throw new KeyNotFoundException($"Inventory item not found for shop {request.ShopId} and drug {request.DrugId}");
        }

        // Update packaging-level prices
        foreach (var (packagingLevel, price) in request.PackagingLevelPrices)
        {
            inventory.ShopPricing.SetPackagingLevelPrice(packagingLevel, price);
        }

        await _repository.UpdateAsync(inventory, cancellationToken);

        _logger.LogInformation("Updated packaging-level pricing for drug {DrugId} in shop {ShopId}",
            request.DrugId, request.ShopId);

        return MapToDto(inventory);
    }

    private static InventoryDto MapToDto(ShopInventory inventory)
    {
        return new InventoryDto
        {
            Id = inventory.Id,
            ShopId = inventory.ShopId,
            DrugId = inventory.DrugId,
            TotalStock = inventory.TotalStock,
            ReorderPoint = inventory.ReorderPoint,
            StorageLocation = inventory.StorageLocation,
            IsAvailable = inventory.IsAvailable,
            LastRestockDate = inventory.LastRestockDate,
            ShopPricing = new ShopPricingDto
            {
                CostPrice = inventory.ShopPricing.CostPrice,
                SellingPrice = inventory.ShopPricing.SellingPrice,
                Discount = inventory.ShopPricing.Discount,
                Currency = inventory.ShopPricing.Currency,
                TaxRate = inventory.ShopPricing.TaxRate,
                LastPriceUpdate = inventory.ShopPricing.LastPriceUpdate,
                PackagingLevelPrices = inventory.ShopPricing.PackagingLevelPrices
            },
            Batches = inventory.Batches.Select(b => new BatchDto
            {
                BatchNumber = b.BatchNumber,
                SupplierId = b.SupplierId ?? string.Empty,
                QuantityOnHand = b.QuantityOnHand,
                ReceivedDate = b.ReceivedDate,
                ExpiryDate = b.ExpiryDate,
                PurchasePrice = b.PurchasePrice,
                SellingPrice = b.SellingPrice,
                Status = b.Status.ToString(),
                Location = b.Location.ToString(),
                StorageLocation = b.StorageLocation
            }).ToList(),
            CreatedAt = inventory.CreatedAt,
            UpdatedAt = inventory.LastUpdated ?? inventory.CreatedAt
        };
    }
}