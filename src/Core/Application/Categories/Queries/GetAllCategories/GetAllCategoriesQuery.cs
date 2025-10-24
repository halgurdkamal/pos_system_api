using MediatR;
using pos_system_api.Core.Application.Categories.DTOs;

namespace pos_system_api.Core.Application.Categories.Queries.GetAllCategories;

public record GetAllCategoriesQuery(bool ActiveOnly = true) : IRequest<IEnumerable<CategoryDto>>;
