namespace pos_system_api.Core.Application.Categories.DTOs;

/// <summary>
/// Category data transfer object
/// </summary>
public record CategoryDto
{
    public string CategoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? Description { get; init; }
    public string? ColorCode { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
    public int DrugCount { get; init; } // Number of drugs in this category
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Simple category DTO for dropdowns/selections
/// </summary>
public record CategorySimpleDto
{
    public string CategoryId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? ColorCode { get; init; }
}

/// <summary>
/// Create category request
/// </summary>
public record CreateCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? Description { get; init; }
    public string? ColorCode { get; init; }
    public int DisplayOrder { get; init; }
}

/// <summary>
/// Update category request
/// </summary>
public record UpdateCategoryDto
{
    public string Name { get; init; } = string.Empty;
    public string? LogoUrl { get; init; }
    public string? Description { get; init; }
    public string? ColorCode { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}
