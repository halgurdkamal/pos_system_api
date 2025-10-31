using Microsoft.EntityFrameworkCore;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Domain.Drugs.Entities;

namespace pos_system_api.Infrastructure.Data.Repositories;

/// <summary>
/// Entity Framework Core repository implementation for Drug entity
/// </summary>
public class DrugRepository : IDrugRepository
{
    private readonly ApplicationDbContext _context;

    public DrugRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Drug?> GetByIdAsync(
        string drugId,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .Drugs.Include(d => d.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DrugId == drugId, cancellationToken);
    }

    public async Task<Drug?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .Drugs.Include(d => d.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Barcode == barcode, cancellationToken);
    }

    public async Task<PagedResult<Drug>> GetAllAsync(
        int page,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var total = await _context.Drugs.CountAsync(cancellationToken);

        var data = await _context
            .Drugs.Include(d => d.Category)
            .AsNoTracking()
            .OrderBy(d => d.BrandName)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return new PagedResult<Drug>(data, page, limit, total);
    }

    public async Task<Drug> CreateAsync(Drug drug, CancellationToken cancellationToken = default)
    {
        drug.CreatedAt = DateTime.UtcNow;
        drug.CreatedBy = "system";

        _context.Drugs.Add(drug);
        await _context.SaveChangesAsync(cancellationToken);

        return drug;
    }

    public async Task<Drug> UpdateAsync(Drug drug, CancellationToken cancellationToken = default)
    {
        var existingDrug = await _context.Drugs.FirstOrDefaultAsync(
            d => d.DrugId == drug.DrugId,
            cancellationToken
        );

        if (existingDrug != null)
        {
            // Update properties
            existingDrug.Barcode = drug.Barcode;
            existingDrug.BarcodeType = drug.BarcodeType;
            existingDrug.BrandName = drug.BrandName;
            existingDrug.GenericName = drug.GenericName;
            existingDrug.Manufacturer = drug.Manufacturer;
            existingDrug.OriginCountry = drug.OriginCountry;
            existingDrug.CategoryId = drug.CategoryId;
            existingDrug.CategoryName = drug.CategoryName;
            existingDrug.ImageUrls = drug.ImageUrls;
            existingDrug.Description = drug.Description;
            existingDrug.SideEffects = drug.SideEffects;
            existingDrug.InteractionNotes = drug.InteractionNotes;
            existingDrug.Tags = drug.Tags;
            existingDrug.RelatedDrugs = drug.RelatedDrugs;
            existingDrug.Formulation = drug.Formulation;
            existingDrug.BasePricing = drug.BasePricing;
            existingDrug.Regulatory = drug.Regulatory;
            existingDrug.LastUpdated = DateTime.UtcNow;
            existingDrug.UpdatedBy = "system";

            await _context.SaveChangesAsync(cancellationToken);
        }

        return existingDrug ?? drug;
    }

    public async Task<bool> DeleteAsync(
        string drugId,
        CancellationToken cancellationToken = default
    )
    {
        var drug = await _context.Drugs.FirstOrDefaultAsync(
            d => d.DrugId == drugId,
            cancellationToken
        );

        if (drug != null)
        {
            _context.Drugs.Remove(drug);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        return false;
    }
}
