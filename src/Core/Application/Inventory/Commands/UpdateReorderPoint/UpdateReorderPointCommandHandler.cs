using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdateReorderPoint;

/// <summary>
/// Handler for UpdateReorderPointCommand
/// </summary>
public class UpdateReorderPointCommandHandler : IRequestHandler<UpdateReorderPointCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateReorderPointCommandHandler(IInventoryRepository inventoryRepository, IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<InventoryDto> Handle(UpdateReorderPointCommand request, CancellationToken cancellationToken)
    {
        // Get inventory
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(
            request.ShopId,
            request.DrugId,
            cancellationToken
        );

        if (inventory == null)
        {
            throw new InvalidOperationException(
                $"Inventory not found for Shop '{request.ShopId}' and Drug '{request.DrugId}'."
            );
        }

        // Validate reorder point
        if (request.ReorderPoint < 0)
        {
            throw new ArgumentException("Reorder point cannot be negative.");
        }

        // Update reorder point
        inventory.ReorderPoint = request.ReorderPoint;
        inventory.LastUpdated = DateTime.UtcNow;

        // Save changes
        var updatedInventory = await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to DTO
        return InventoryMapper.MapToDto(updatedInventory);
    }
}
