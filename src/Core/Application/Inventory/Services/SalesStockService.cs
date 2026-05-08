using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Sales.Entities;
using pos_system_api.Core.Domain.Sales.ValueObjects;

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

    // F-2/F-3: this service only stages changes via IInventoryRepository.UpdateAsync;
    // the calling command handler commits with a single SaveChangesAsync, so the
    // order status flip and the per-batch inventory mutations land in one EF-managed
    // transaction. Do NOT call SaveChangesAsync from here — a second commit would
    // either be a no-op or, worse, split the writes if a handler rearranged its calls.
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

            var unitsToDeduct = ResolveBaseUnits(item);

            // F-8: refuse the payment outright when stock is insufficient. The
            // domain ReduceStock silently caps at zero, which used to mean a
            // payment succeeded with the customer charged for goods that didn't
            // exist. The surrounding Q-6 transaction rolls back the order status
            // flip on this throw, so the order stays Confirmed.
            if (inventory.TotalStock < unitsToDeduct)
            {
                throw new InvalidOperationException(
                    $"Insufficient stock for drug {item.DrugId} in shop {order.ShopId}: " +
                    $"available {inventory.TotalStock}, requested {unitsToDeduct}.");
            }

            var perBatch = inventory.ReduceStock(unitsToDeduct);
            item.RecordBatchDeductions(perBatch.Select(d =>
                new SalesOrderItemBatchDeduction(d.BatchNumber, d.Quantity)));
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

            _logger.LogInformation(
                "Deducted {BaseUnits} base units of {DrugId} for order {OrderNumber} (shop {ShopId}) across {BatchCount} batch(es); remaining stock {Remaining}",
                unitsToDeduct, item.DrugId, order.OrderNumber, order.ShopId, perBatch.Count, inventory.TotalStock);
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

            int unitsToRestore;
            if (item.BatchDeductions.Count > 0)
            {
                // Precise restore: credit each FIFO chunk back to its source batch.
                inventory.RestoreStockToBatches(
                    item.BatchDeductions.Select(d => (d.BatchNumber, d.Quantity)));
                unitsToRestore = item.BatchDeductions.Sum(d => d.Quantity);
            }
            else
            {
                // Legacy fallback: orders paid before per-batch tracking shipped.
                // Best-effort restore using the line's recorded BatchNumber (often
                // null) — this is the path that historically dropped restored units
                // onto the most recently received batch.
                unitsToRestore = ResolveBaseUnits(item);
                inventory.RestoreStock(unitsToRestore, item.BatchNumber);
            }
            await _inventoryRepository.UpdateAsync(inventory, cancellationToken);

            _logger.LogInformation(
                "Restored {BaseUnits} base units of {DrugId} for reversal of order {OrderNumber} (shop {ShopId}); new stock {Stock}",
                unitsToRestore, item.DrugId, order.OrderNumber, order.ShopId, inventory.TotalStock);
        }
    }

    // BaseUnitsConsumed is the authoritative deduction unit (a sale of "1 Box" of 100
    // tablets sets Quantity=1 and BaseUnitsConsumed=100; ShopInventory and Batch track
    // base units, so we must deduct 100, not 1). SalesOrders predating the Nov-2025
    // schema migration have BaseUnitsConsumed=0; falling back to Quantity preserves
    // the (self-consistent) behaviour they were paid against.
    private static int ResolveBaseUnits(SalesOrderItem item) =>
        item.BaseUnitsConsumed > 0
            ? (int)Math.Round(item.BaseUnitsConsumed, MidpointRounding.ToEven)
            : item.Quantity;

    // Q-6: Loads with SELECT ... FOR UPDATE so concurrent payment / refund / cancel
    // handlers serialise on the same (shop, drug) rows and can't race past stock
    // checks. The caller MUST be inside an IUnitOfWork transaction; the three
    // sales handlers (ProcessPayment / Refund / Cancel) all open one.
    private async Task<IDictionary<string, Core.Domain.Inventory.Entities.ShopInventory>> LoadInventoriesAsync(
        SalesOrder order,
        CancellationToken cancellationToken)
    {
        var drugIds = order.Items
            .Select(i => i.DrugId)
            .Distinct()
            .ToList();

        var rows = await _inventoryRepository.GetByShopAndDrugsForUpdateAsync(
            order.ShopId, drugIds, cancellationToken);

        return rows.ToDictionary(i => i.DrugId, StringComparer.OrdinalIgnoreCase);
    }
}
