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
            ShopPricing = new ShopPricingDto
            {
                CostPrice = inventory.ShopPricing.CostPrice,
                SellingPrice = inventory.ShopPricing.SellingPrice,
                Currency = inventory.ShopPricing.Currency,
                TaxRate = inventory.ShopPricing.TaxRate
            },
            CreatedAt = inventory.CreatedAt,
            UpdatedAt = inventory.LastUpdated ?? inventory.CreatedAt
        };
    }
}
