using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

public interface IInventoryAlertService
{
    Task GenerateAlertsForShopAsync(string shopId, CancellationToken cancellationToken = default);
    Task GenerateAlertsForDrugAsync(string shopId, string drugId, CancellationToken cancellationToken = default);
    Task CheckLowStockAsync(string shopId, CancellationToken cancellationToken = default);
    Task CheckExpiringItemsAsync(string shopId, CancellationToken cancellationToken = default);
    Task AutoResolveAlertsAsync(string shopId, CancellationToken cancellationToken = default);
}
