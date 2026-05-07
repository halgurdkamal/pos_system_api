using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Sales.Entities;

namespace pos_system_api.Core.Application.Inventory.Services;

public class SalesStockService : ISalesStockService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ILogger<SalesStockService> _logger;

    public SalesStockService(
        IInventoryRepository inventoryRepository,
        ILogger<SalesStockService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _logger = logger;
    }

    public async Task DeductForSaleAsync(SalesOrder order, CancellationToken cancellationToken = default)
    {
        if (order.Items.Count == 0)
        {
            return;
        }

        var inventories = await LoadInventoriesAsync(order, cancellationToken);

        foreach (var item in order.Items)
        {
            if (!inventories.TryGetValue(item.DrugId, out var inventory))
            {
                // No inventory record for this (shop, drug). Don't block payment —
                // the cashier may be selling something the inventory module hasn't
                // been told about yet (e.g., backorder, custom item). Log so it's
                // visible.
                _logger.LogWarning(
                    "ShopInventory missing for sale of drug {DrugId} in shop {ShopId} (order {OrderNumber}); stock not adjusted",
                    item.DrugId, order.ShopId, order.OrderNumber);
                continue;
            }

            inventory.ReduceStock(item.Quantity);
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

            _logger.LogInformation(
                "Deducted {Quantity} of {DrugId} for order {OrderNumber} (shop {ShopId}); remaining stock {Remaining}",
                item.Quantity, item.DrugId, order.OrderNumber, order.ShopId, inventory.TotalStock);
        }
    }

    public async Task RestoreForReversalAsync(SalesOrder order, CancellationToken cancellationToken = default)
    {
        if (order.Items.Count == 0)
        {
            return;
        }

        var inventories = await LoadInventoriesAsync(order, cancellationToken);

        foreach (var item in order.Items)
        {
            if (!inventories.TryGetValue(item.DrugId, out var inventory))
            {
                _logger.LogWarning(
                    "ShopInventory missing for refund/cancel of drug {DrugId} in shop {ShopId} (order {OrderNumber}); stock not restored",
                    item.DrugId, order.ShopId, order.OrderNumber);
                continue;
            }

            inventory.RestoreStock(item.Quantity, item.BatchNumber);
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

            _logger.LogInformation(
                "Restored {Quantity} of {DrugId} for reversal of order {OrderNumber} (shop {ShopId}); new stock {Stock}",
                item.Quantity, item.DrugId, order.OrderNumber, order.ShopId, inventory.TotalStock);
        }
    }

    private async Task<IDictionary<string, Core.Domain.Inventory.Entities.ShopInventory>> LoadInventoriesAsync(
        SalesOrder order,
        CancellationToken cancellationToken)
    {
        var drugIds = order.Items
            .Select(i => i.DrugId)
            .Distinct()
            .ToList();

        var rows = await _inventoryRepository.GetByShopAndDrugsAsync(
            order.ShopId, drugIds, cancellationToken);

        return rows.ToDictionary(i => i.DrugId, StringComparer.OrdinalIgnoreCase);
    }
}
