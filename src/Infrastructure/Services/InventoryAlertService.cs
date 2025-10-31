using Microsoft.Extensions.Logging;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Services;

public class InventoryAlertService : IInventoryAlertService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryAlertRepository _alertRepository;
    private readonly ILogger<InventoryAlertService> _logger;

    public InventoryAlertService(
        IInventoryRepository inventoryRepository,
        IInventoryAlertRepository alertRepository,
        ILogger<InventoryAlertService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _alertRepository = alertRepository;
        _logger = logger;
    }

    public async Task GenerateAlertsForShopAsync(string shopId, CancellationToken cancellationToken = default)
    {
        await CheckLowStockAsync(shopId, cancellationToken);
        await CheckExpiringItemsAsync(shopId, cancellationToken);
        await AutoResolveAlertsAsync(shopId, cancellationToken);
    }

    public async Task GenerateAlertsForDrugAsync(string shopId, string drugId, CancellationToken cancellationToken = default)
    {
        var inventory = await _inventoryRepository.GetByShopAndDrugAsync(shopId, drugId, cancellationToken);
        if (inventory == null) return;

        await CheckLowStockForInventory(inventory, cancellationToken);
        await CheckExpiringBatchesForInventory(inventory, cancellationToken);
    }

    public async Task CheckLowStockAsync(string shopId, CancellationToken cancellationToken = default)
    {
        var inventories = await _inventoryRepository.GetAllByShopAsync(shopId, cancellationToken);

        foreach (var inventory in inventories)
        {
            await CheckLowStockForInventory(inventory, cancellationToken);
        }
    }

    public async Task CheckExpiringItemsAsync(string shopId, CancellationToken cancellationToken = default)
    {
        var inventories = await _inventoryRepository.GetAllByShopAsync(shopId, cancellationToken);

        foreach (var inventory in inventories)
        {
            await CheckExpiringBatchesForInventory(inventory, cancellationToken);
        }
    }

    public async Task AutoResolveAlertsAsync(string shopId, CancellationToken cancellationToken = default)
    {
        var activeAlerts = await _alertRepository.GetActiveAlertsAsync(shopId, cancellationToken: cancellationToken);
        var inventories = await _inventoryRepository.GetAllByShopAsync(shopId, cancellationToken);

        foreach (var alert in activeAlerts)
        {
            bool shouldResolve = false;
            string? resolutionNote = null;

            var inventory = inventories.FirstOrDefault(i => i.DrugId == alert.DrugId);
            if (inventory == null) continue;

            switch (alert.AlertType)
            {
                case AlertType.LowStock:
                    if (inventory.TotalStock >= inventory.ReorderPoint)
                    {
                        shouldResolve = true;
                        resolutionNote = "Stock level restored above reorder point";
                    }
                    break;

                case AlertType.OutOfStock:
                    if (inventory.TotalStock > 0)
                    {
                        shouldResolve = true;
                        resolutionNote = "Stock replenished";
                    }
                    break;

                case AlertType.Expired:
                case AlertType.ExpiringSoon30Days:
                case AlertType.ExpiringSoon60Days:
                case AlertType.ExpiringSoon90Days:
                    if (alert.BatchNumber != null)
                    {
                        var batch = inventory.Batches.FirstOrDefault(b => b.BatchNumber == alert.BatchNumber);
                        if (batch == null || batch.QuantityOnHand == 0)
                        {
                            shouldResolve = true;
                            resolutionNote = "Batch removed or depleted";
                        }
                    }
                    break;
            }

            if (shouldResolve)
            {
                alert.Resolve("System", resolutionNote);
                await _alertRepository.UpdateAsync(alert, cancellationToken);
                _logger.LogInformation("Auto-resolved alert {AlertId} for shop {ShopId}: {Note}",
                    alert.Id, shopId, resolutionNote);
            }
        }
    }

    private async Task CheckLowStockForInventory(ShopInventory inventory, CancellationToken cancellationToken)
    {
        var existingAlerts = await _alertRepository.GetAlertsByDrugAsync(inventory.ShopId, inventory.DrugId, cancellationToken);
        var hasActiveLowStockAlert = existingAlerts.Any(a =>
            a.Status == AlertStatus.Active &&
            (a.AlertType == AlertType.LowStock || a.AlertType == AlertType.OutOfStock));

        if (inventory.TotalStock == 0 && !hasActiveLowStockAlert)
        {
            var alert = new InventoryAlert(
                inventory.ShopId,
                inventory.DrugId,
                null,
                AlertType.OutOfStock,
                AlertSeverity.Critical,
                "Drug is out of stock",
                0,
                inventory.ReorderPoint);

            await _alertRepository.AddAsync(alert, cancellationToken);
            _logger.LogWarning("Out of stock alert generated for drug {DrugId} in shop {ShopId}",
                inventory.DrugId, inventory.ShopId);
        }
        else if (inventory.TotalStock > 0 && inventory.TotalStock <= inventory.ReorderPoint && !hasActiveLowStockAlert)
        {
            var alert = new InventoryAlert(
                inventory.ShopId,
                inventory.DrugId,
                null,
                AlertType.LowStock,
                AlertSeverity.Warning,
                $"Stock level ({inventory.TotalStock}) is below reorder point ({inventory.ReorderPoint})",
                inventory.TotalStock,
                inventory.ReorderPoint);

            await _alertRepository.AddAsync(alert, cancellationToken);
            _logger.LogWarning("Low stock alert generated for drug {DrugId} in shop {ShopId}",
                inventory.DrugId, inventory.ShopId);
        }
    }

    private async Task CheckExpiringBatchesForInventory(ShopInventory inventory, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var existingAlerts = await _alertRepository.GetAlertsByDrugAsync(inventory.ShopId, inventory.DrugId, cancellationToken);

        foreach (var batch in inventory.Batches.Where(b => b.QuantityOnHand > 0))
        {
            var expiryDate = batch.ExpiryDate;
            var daysUntilExpiry = (expiryDate - now).TotalDays;

            AlertType? alertType = null;
            AlertSeverity severity = AlertSeverity.Info;
            string message = string.Empty;

            if (daysUntilExpiry < 0)
            {
                alertType = AlertType.Expired;
                severity = AlertSeverity.Critical;
                message = $"Batch {batch.BatchNumber} has expired on {expiryDate:yyyy-MM-dd}";
            }
            else if (daysUntilExpiry <= 30)
            {
                alertType = AlertType.ExpiringSoon30Days;
                severity = AlertSeverity.Critical;
                message = $"Batch {batch.BatchNumber} expires in {(int)daysUntilExpiry} days ({expiryDate:yyyy-MM-dd})";
            }
            else if (daysUntilExpiry <= 60)
            {
                alertType = AlertType.ExpiringSoon60Days;
                severity = AlertSeverity.Warning;
                message = $"Batch {batch.BatchNumber} expires in {(int)daysUntilExpiry} days ({expiryDate:yyyy-MM-dd})";
            }
            else if (daysUntilExpiry <= 90)
            {
                alertType = AlertType.ExpiringSoon90Days;
                severity = AlertSeverity.Info;
                message = $"Batch {batch.BatchNumber} expires in {(int)daysUntilExpiry} days ({expiryDate:yyyy-MM-dd})";
            }

            if (alertType.HasValue)
            {
                var hasActiveAlert = existingAlerts.Any(a =>
                    a.Status == AlertStatus.Active &&
                    a.AlertType == alertType.Value &&
                    a.BatchNumber == batch.BatchNumber);

                if (!hasActiveAlert)
                {
                    var alert = new InventoryAlert(
                        inventory.ShopId,
                        inventory.DrugId,
                        batch.BatchNumber,
                        alertType.Value,
                        severity,
                        message,
                        batch.QuantityOnHand,
                        null,
                        expiryDate);

                    await _alertRepository.AddAsync(alert, cancellationToken);
                    _logger.LogInformation("Expiry alert generated for batch {BatchNumber} of drug {DrugId} in shop {ShopId}",
                        batch.BatchNumber, inventory.DrugId, inventory.ShopId);
                }
            }
        }
    }
}
