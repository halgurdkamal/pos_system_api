using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.Entities;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Core.Application.Inventory.Commands.AddStock;

/// <summary>
/// Handler for AddStockCommand
/// </summary>
public class AddStockCommandHandler : IRequestHandler<AddStockCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;

    public AddStockCommandHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<InventoryDto> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        // Check if inventory already exists for this shop-drug combination
        var existingInventory = await _inventoryRepository.GetByShopAndDrugAsync(
            request.ShopId,
            request.DrugId,
            cancellationToken
        );

        ShopInventory inventory;

        if (existingInventory == null)
        {
            // Create new inventory with shop-specific pricing
            var shopPricing = new ShopPricing
            {
                CostPrice = request.PurchasePrice,
                SellingPrice = request.SellingPrice,
                Currency = "USD",
                TaxRate = 0.0m // Can be configured per shop
            };

            inventory = new ShopInventory(
                shopId: request.ShopId,
                drugId: request.DrugId,
                reorderPoint: request.ReorderPoint ?? 50,
                storageLocation: request.StorageLocation,
                shopPricing: shopPricing
            );

            // Add the batch
            var batch = new Batch
            {
                BatchNumber = request.BatchNumber,
                SupplierId = request.SupplierId,
                QuantityOnHand = request.Quantity,
                ReceivedDate = DateTime.UtcNow,
                ExpiryDate = request.ExpiryDate,
                PurchasePrice = request.PurchasePrice,
                SellingPrice = request.SellingPrice,
                Status = BatchStatus.Active
            };

            inventory.AddBatch(batch);

            // Save new inventory
            inventory = await _inventoryRepository.AddAsync(inventory, cancellationToken);
        }
        else
        {
            // Add batch to existing inventory
            var batch = new Batch
            {
                BatchNumber = request.BatchNumber,
                SupplierId = request.SupplierId,
                QuantityOnHand = request.Quantity,
                ReceivedDate = DateTime.UtcNow,
                ExpiryDate = request.ExpiryDate,
                PurchasePrice = request.PurchasePrice,
                SellingPrice = request.SellingPrice,
                Status = BatchStatus.Active
            };

            existingInventory.AddBatch(batch);

            // Update storage location if provided
            if (!string.IsNullOrWhiteSpace(request.StorageLocation))
            {
                existingInventory.StorageLocation = request.StorageLocation;
            }

            inventory = await _inventoryRepository.UpdateAsync(existingInventory, cancellationToken);
        }

        // Map to DTO
        return InventoryMapper.MapToDto(inventory);
    }
}
