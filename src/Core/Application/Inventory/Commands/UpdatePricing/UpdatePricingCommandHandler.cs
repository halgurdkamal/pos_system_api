using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Inventory.DTOs;
using pos_system_api.Core.Domain.Inventory.ValueObjects;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdatePricing;

/// <summary>
/// Handler for UpdatePricingCommand
/// </summary>
public class UpdatePricingCommandHandler : IRequestHandler<UpdatePricingCommand, InventoryDto>
{
    private readonly IInventoryRepository _inventoryRepository;

    public UpdatePricingCommandHandler(IInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<InventoryDto> Handle(UpdatePricingCommand request, CancellationToken cancellationToken)
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

        // Create new pricing
        var newPricing = new ShopPricing
        {
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            Currency = inventory.ShopPricing.Currency, // Keep existing currency
            TaxRate = request.TaxRate ?? inventory.ShopPricing.TaxRate // Keep existing if not provided
        };

        // Update pricing using domain method
        inventory.UpdatePricing(newPricing);

        // Save changes
        var updatedInventory = await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

        // Map to DTO
        return InventoryMapper.MapToDto(updatedInventory);
    }
}
