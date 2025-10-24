using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Categories.DTOs;

namespace pos_system_api.Core.Application.Categories.Queries.GetAllCategories;

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, IEnumerable<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetAllCategoriesQueryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<IEnumerable<CategoryDto>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllAsync(request.ActiveOnly, cancellationToken);

        return categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(category => new CategoryDto
            {
                CategoryId = category.CategoryId,
                Name = category.Name,
                LogoUrl = category.LogoUrl,
                Description = category.Description,
                ColorCode = category.ColorCode,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                DrugCount = 0, // Will be populated separately if needed
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.LastUpdated
            });
    }
}
