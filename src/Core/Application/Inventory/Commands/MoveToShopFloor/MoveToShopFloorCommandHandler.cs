using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Commands.MoveToShopFloor;

public class MoveToShopFloorCommandHandler : IRequestHandler<MoveToShopFloorCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<MoveToShopFloorCommandHandler> _logger;

    public MoveToShopFloorCommandHandler(
        IInventoryRepository inventoryRepository,
        ILogger<MoveToShopFloorCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<InventoryDto> Handle(MoveToShopFloorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Moving {Quantity} units from storage to shop floor for drug {DrugId} in shop {ShopId}",
            request.Quantity, request.DrugId, request.ShopId);

        // Get inventory
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(
            request.ShopId,
            request.DrugId,
            cancellationToken);

        if (inventory == null)
        {
            _logger.LogWarning("Inventory not found for drug {DrugId} in shop {ShopId}",
                request.DrugId, request.ShopId);
            throw new KeyNotFoundException($"Inventory not found for drug {request.DrugId} in shop {request.ShopId}");
        }

        // Check if enough stock in storage
        var storageStock = inventory.GetStorageStock();
        if (storageStock < request.Quantity)
        {
            _logger.LogWarning("Insufficient storage stock. Requested: {Requested}, Available: {Available}",
                request.Quantity, storageStock);
            throw new InvalidOperationException(
                $"Insufficient stock in storage. Requested: {request.Quantity}, Available: {storageStock}");
        }

        // Move stock using FEFO (First Expired First Out)
        inventory.RestockShopFloor(request.Quantity, request.BatchNumber);

        // Save changes
        var updatedInventory = await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

        _logger.LogInformation("Successfully moved {Quantity} units to shop floor. Shop floor stock: {ShopFloorStock}, Storage stock: {StorageStock}",
            request.Quantity, updatedInventory.GetShopFloorStock(), updatedInventory.GetStorageStock());

        return InventoryMapper.MapToDto(updatedInventory);
    }
}
