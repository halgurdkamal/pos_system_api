using MediatR;
using pos_system_api.Core.Application.Common.Interfaces;
using pos_system_api.Core.Application.Categories.DTOs;
using FluentValidation;
using pos_system_api.Core.Domain.Categories.Entities;

namespace pos_system_api.Core.Application.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? LogoUrl = null,
    string? Description = null,
    string? ColorCode = null,
    int DisplayOrder = 0
) : IRequest<CategoryDto>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters");

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500).WithMessage("Logo URL cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.LogoUrl));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ColorCode)
            .Matches(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")
            .WithMessage("Color code must be a valid hex color (e.g., #FF5733)")
            .When(x => !string.IsNullOrEmpty(x.ColorCode));

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be 0 or greater");
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;

    public CreateCategoryCommandHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Check if category with same name already exists
        var existing = await _categoryRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"Category with name '{request.Name}' already exists");
        }

        var category = new Category(request.Name, request.LogoUrl, request.Description)
        {
            ColorCode = request.ColorCode,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _categoryRepository.AddAsync(category, cancellationToken);

        return new CategoryDto
        {
            CategoryId = created.CategoryId,
            Name = created.Name,
            LogoUrl = created.LogoUrl,
            Description = created.Description,
            ColorCode = created.ColorCode,
            DisplayOrder = created.DisplayOrder,
            IsActive = created.IsActive,
            DrugCount = 0,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.LastUpdated
        };
    }
}
