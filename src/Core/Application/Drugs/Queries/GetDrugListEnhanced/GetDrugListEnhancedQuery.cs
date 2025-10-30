using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Common.Models;
using pos_system_api.Core.Application.Drugs.DTOs;

namespace pos_system_api.Core.Application.Drugs.Queries.GetDrugListEnhanced;

public record GetDrugListEnhancedQuery(
    int Page = 1,
    int Limit = 20,
    string? SearchTerm = null,
    string? Category = null,
    bool? InStock = null
) : IRequest<PagedResult<DrugListItemDto>>;

public class GetDrugListEnhancedQueryHandler : IRequestHandler<GetDrugListEnhancedQuery, PagedResult<DrugListItemDto>>
{
    private readonly IDrugRepository _drugRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICategoryRepository _categoryRepository;

    public GetDrugListEnhancedQueryHandler(
        IDrugRepository drugRepository,
        IInventoryRepository inventoryRepository,
        ICategoryRepository categoryRepository)
    {
        _drugRepository = drugRepository;
        _inventoryRepository = inventoryRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<PagedResult<DrugListItemDto>> Handle(GetDrugListEnhancedQuery request, CancellationToken cancellationToken)
    {
        // Get paginated drugs
        var drugsResult = await _drugRepository.GetAllAsync(request.Page, request.Limit, cancellationToken);
        
        // Get categories for logo/color lookup
        var allCategories = await _categoryRepository.GetAllAsync(true, cancellationToken);
        var categoriesById = allCategories.ToDictionary(c => c.CategoryId, StringComparer.OrdinalIgnoreCase);
        
        // Map to list DTOs (simplified - stock data would require shop context)
        var items = drugsResult.Data.Select(drug =>
        {
            var categoryInfo = categoriesById.GetValueOrDefault(drug.CategoryId);
            
            return new DrugListItemDto
            {
                DrugId = drug.Id,
                BrandName = drug.BrandName,
                GenericName = drug.GenericName,
                Barcode = drug.Barcode,
                CategoryId = drug.CategoryId,
                Category = categoryInfo?.Name ?? drug.CategoryName,
                CategoryLogoUrl = categoryInfo?.LogoUrl,
                CategoryColorCode = categoryInfo?.ColorCode,
                PrimaryImageUrl = drug.ImageUrls.FirstOrDefault(),
                Manufacturer = drug.Manufacturer,
                Strength = drug.Formulation.Strength,
                Form = drug.Formulation.Form,
                SuggestedRetailPrice = drug.BasePricing.SuggestedRetailPrice,
                WholesalePrice = 0, // Not in BasePricing, can add later
                TotalQuantityInStock = 0, // Will be populated in detail view
                ShopCount = 0, // Will be populated in detail view
                IsAvailable = true, // Default to true for catalog
                RequiresPrescription = drug.Regulatory.IsPrescriptionRequired
            };
        }).ToList();
        
        // Apply filters
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            items = items.Where(d =>
                d.BrandName.ToLower().Contains(searchLower) ||
                d.GenericName.ToLower().Contains(searchLower) ||
                d.Barcode.ToLower().Contains(searchLower) ||
                d.Manufacturer.ToLower().Contains(searchLower)).ToList();
        }
        
        if (!string.IsNullOrEmpty(request.Category))
        {
            items = items.Where(d =>
                d.CategoryId.Equals(request.Category, StringComparison.OrdinalIgnoreCase) ||
                d.Category.Equals(request.Category, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        
        return new PagedResult<DrugListItemDto>
        {
            Data = items,
            Total = drugsResult.Total,
            Page = request.Page,
            Limit = request.Limit
        };
    }
}
