using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Domain.Suppliers.Entities;

namespace pos_system_api.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Supplier entity
/// </summary>
public class SupplierRepository : ISupplierRepository
{
    private readonly ApplicationDbContext _context;

    public SupplierRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Supplier?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Supplier> Suppliers, int TotalCount)> GetAllAsync(
        int page,
        int limit,
        bool? isActive = null,
        SupplierType? supplierType = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(s => s.IsActive == isActive.Value);
        }

        if (supplierType.HasValue)
        {
            query = query.Where(s => s.SupplierType == supplierType.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var suppliers = await query
            .OrderBy(s => s.SupplierName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (suppliers, totalCount);
    }

    public async Task<IEnumerable<Supplier>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.SupplierName.ToLower().Contains(searchTerm.ToLower()))
            .OrderBy(s => s.SupplierName)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Supplier>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SupplierName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Supplier> AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await _context.Suppliers.AddAsync(supplier, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public async Task<Supplier> UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers.FindAsync(new object[] { id }, cancellationToken);
        if (supplier != null)
        {
            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        string? excludeSupplierId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.Where(s => s.Email == email);

        if (!string.IsNullOrEmpty(excludeSupplierId))
        {
            query = query.Where(s => s.Id != excludeSupplierId);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
