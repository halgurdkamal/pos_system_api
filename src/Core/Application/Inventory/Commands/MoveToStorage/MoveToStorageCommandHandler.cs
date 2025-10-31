using MediatR;
using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Commands.MoveToStorage;

public class MoveToStorageCommandHandler : IRequestHandler<MoveToStorageCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<MoveToStorageCommandHandler> _logger;

    public MoveToStorageCommandHandler(
        IInventoryRepository inventoryRepository,
        ILogger<MoveToStorageCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task<InventoryDto> Handle(MoveToStorageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Moving {Quantity} units from shop floor to storage for drug {DrugId} in shop {ShopId}",
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

        // Check if enough stock on shop floor
        var shopFloorStock = inventory.GetShopFloorStock();
        if (shopFloorStock < request.Quantity)
        {
            _logger.LogWarning("Insufficient shop floor stock. Requested: {Requested}, Available: {Available}",
                request.Quantity, shopFloorStock);
            throw new InvalidOperationException(
                $"Insufficient stock on shop floor. Requested: {request.Quantity}, Available: {shopFloorStock}");
        }

        // Move stock back to storage
        inventory.ReturnToStorage(request.Quantity, request.BatchNumber);

        // Save changes
        var updatedInventory = await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

        _logger.LogInformation("Successfully moved {Quantity} units to storage. Shop floor stock: {ShopFloorStock}, Storage stock: {StorageStock}",
            request.Quantity, updatedInventory.GetShopFloorStock(), updatedInventory.GetStorageStock());

        return InventoryMapper.MapToDto(updatedInventory);
    }
}
