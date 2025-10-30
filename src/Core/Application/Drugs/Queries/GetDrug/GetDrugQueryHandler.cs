using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Domain.Drugs.Entities;

namespace pos_system_api.Core.Application.Drugs.Queries.GetDrug;

/// <summary>
/// Handler for GetDrugQuery
/// </summary>
public class GetDrugQueryHandler : IRequestHandler<GetDrugQuery, DrugDto?>
{
    private readonly IDrugRepository _drugRepository;

    public GetDrugQueryHandler(IDrugRepository drugRepository)
    {
        _drugRepository = drugRepository;
    }

    public async Task<DrugDto?> Handle(GetDrugQuery request, CancellationToken cancellationToken)
    {
        var drug = await _drugRepository.GetByIdAsync(request.DrugId, cancellationToken);
        
        if (drug == null)
            return null;

        return MapToDto(drug);
    }

    private static DrugDto MapToDto(Drug drug)
    {
        return new DrugDto
        {
            DrugId = drug.DrugId,
            Barcode = drug.Barcode,
            BarcodeType = drug.BarcodeType,
            BrandName = drug.BrandName,
            GenericName = drug.GenericName,
            Manufacturer = drug.Manufacturer,
            OriginCountry = drug.OriginCountry,
            CategoryId = drug.CategoryId,
            Category = drug.Category?.Name ?? drug.CategoryName,
            ImageUrls = drug.ImageUrls,
            Description = drug.Description,
            SideEffects = drug.SideEffects,
            InteractionNotes = drug.InteractionNotes,
            Tags = drug.Tags,
            RelatedDrugs = drug.RelatedDrugs,
            Formulation = drug.Formulation,
            BasePricing = drug.BasePricing,
            Regulatory = drug.Regulatory,
            // NOTE: Inventory and supplier info are now shop-specific (see ShopInventory)
            CreatedAt = drug.CreatedAt,
            CreatedBy = drug.CreatedBy,
            LastUpdated = drug.LastUpdated,
            UpdatedBy = drug.UpdatedBy
        };
    }
}
