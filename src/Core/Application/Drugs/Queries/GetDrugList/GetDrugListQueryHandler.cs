using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Domain.Drugs.Entities;

namespace pos_system_api.Core.Application.Drugs.Queries.GetDrugList;

/// <summary>
/// Handler for GetDrugListQuery
/// </summary>
public class GetDrugListQueryHandler : IRequestHandler<GetDrugListQuery, PagedResult<DrugDto>>
{
    private readonly IDrugRepository _drugRepository;

    public GetDrugListQueryHandler(IDrugRepository drugRepository)
    {
        _drugRepository = drugRepository;
    }

    public async Task<PagedResult<DrugDto>> Handle(GetDrugListQuery request, CancellationToken cancellationToken)
    {
        var result = await _drugRepository.GetAllAsync(request.Page, request.Limit, cancellationToken);
        
        var drugDtos = result.Data.Select(MapToDto).ToList();
        
        return new PagedResult<DrugDto>(
            drugDtos,
            result.Page,
            result.Limit,
            result.Total
        );
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
            CreatedAt = drug.CreatedAt,
            CreatedBy = drug.CreatedBy,
            LastUpdated = drug.LastUpdated,
            UpdatedBy = drug.UpdatedBy
        };
    }
}
