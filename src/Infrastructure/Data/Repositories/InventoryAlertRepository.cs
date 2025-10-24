using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Inventory.Entities;

namespace pos_system_api.Infrastructure.Data.Repositories;

public class InventoryAlertRepository : IInventoryAlertRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryAlertRepository(ApplicationDbContext context) => _context = context;

    public async Task<InventoryAlert> AddAsync(InventoryAlert alert, CancellationToken cancellationToken = default)
    {
        await _context.InventoryAlerts.AddAsync(alert, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<InventoryAlert?> GetByIdAsync(string alertId, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryAlerts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
    }

    public async Task UpdateAsync(InventoryAlert alert, CancellationToken cancellationToken = default)
    {
        _context.InventoryAlerts.Update(alert);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryAlert>> GetActiveAlertsAsync(
        string shopId,
        AlertSeverity? severity = null,
        AlertType? alertType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryAlerts
            .AsNoTracking()
            .Where(a => a.ShopId == shopId && a.Status == AlertStatus.Active);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        if (alertType.HasValue)
            query = query.Where(a => a.AlertType == alertType.Value);

        return await query
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryAlert>> GetAlertHistoryAsync(
        string shopId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        AlertStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryAlerts
            .AsNoTracking()
            .Where(a => a.ShopId == shopId);

        if (fromDate.HasValue)
            query = query.Where(a => a.GeneratedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(a => a.GeneratedAt <= toDate.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query
            .OrderByDescending(a => a.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<InventoryAlert>> GetAlertsByDrugAsync(
        string shopId,
        string drugId,
        CancellationToken cancellationToken = default)
    {
        return await _context.InventoryAlerts
            .AsNoTracking()
            .Where(a => a.ShopId == shopId && a.DrugId == drugId)
            .OrderByDescending(a => a.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetActiveAlertCountAsync(
        string shopId,
        AlertSeverity? severity = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryAlerts
            .AsNoTracking()
            .Where(a => a.ShopId == shopId && a.Status == AlertStatus.Active);

        if (severity.HasValue)
            query = query.Where(a => a.Severity == severity.Value);

        return await query.CountAsync(cancellationToken);
    }
}
