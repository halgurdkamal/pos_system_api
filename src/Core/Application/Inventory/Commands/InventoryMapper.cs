using System.Collections.Generic;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Inventory.Commands;

/// <summary>
/// Shared mapper for ShopInventory entity to InventoryDto
/// </summary>
public static class InventoryMapper
{
    public static InventoryDto MapToDto(ShopInventory inventory)
    {
        return new InventoryDto
        {
            Id = inventory.Id,
            ShopId = inventory.ShopId,
            DrugId = inventory.DrugId,
            TotalStock = inventory.TotalStock,
            ShopFloorStock = inventory.GetShopFloorStock(),
            StorageStock = inventory.GetStorageStock(),
            ReservedStock = inventory.GetReservedStock(),
            QuarantinedStock = inventory.GetQuarantinedStock(),
            ReorderPoint = inventory.ReorderPoint,
            StorageLocation = inventory.StorageLocation,
            IsAvailable = inventory.IsAvailable,
            LastRestockDate = inventory.LastRestockDate,
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
            ShopPricing = MapPricing(inventory.ShopPricing),
            Packaging = null,
            CreatedAt = inventory.CreatedAt,
            UpdatedAt = inventory.LastUpdated ?? inventory.CreatedAt
        };
    }

    private static ShopPricingDto MapPricing(pos_system_api.Core.Domain.Inventory.ValueObjects.ShopPricing pricing)
    {
        var packagingPrices = pricing.PackagingLevelPrices ?? new Dictionary<string, decimal>();
        var finalPrice = pricing.GetFinalPrice();

        return new ShopPricingDto
        {
            CostPrice = pricing.CostPrice,
            SellingPrice = pricing.SellingPrice,
            Discount = pricing.Discount,
            Currency = pricing.Currency,
            TaxRate = pricing.TaxRate,
            ProfitMargin = finalPrice - pricing.CostPrice,
            ProfitMarginPercentage = pricing.GetProfitMargin(),
            LastPriceUpdate = pricing.LastPriceUpdate,
            PackagingLevelPrices = new Dictionary<string, decimal>(packagingPrices)
        };
    }
}
