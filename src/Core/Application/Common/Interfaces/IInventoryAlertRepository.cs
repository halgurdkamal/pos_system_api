using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Core.Application.Common.Interfaces;

public interface IInventoryAlertRepository
{
    Task<InventoryAlert> AddAsync(InventoryAlert alert, CancellationToken cancellationToken = default);
    Task<InventoryAlert?> GetByIdAsync(string alertId, CancellationToken cancellationToken = default);
    Task UpdateAsync(InventoryAlert alert, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryAlert>> GetActiveAlertsAsync(string shopId, AlertSeverity? severity = null, AlertType? alertType = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryAlert>> GetAlertHistoryAsync(string shopId, DateTime? fromDate = null, DateTime? toDate = null, AlertStatus? status = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<InventoryAlert>> GetAlertsByDrugAsync(string shopId, string drugId, CancellationToken cancellationToken = default);
    Task<int> GetActiveAlertCountAsync(string shopId, AlertSeverity? severity = null, CancellationToken cancellationToken = default);
}
