using pos_system_api.Core.Domain.Common;
using pos_system_api.Core.Domain.Drugs.Entities;

namespace pos_system_api.Core.Domain.Categories.Entities;

/// <summary>
/// Drug category entity with ID, name, and logo
/// </summary>
public class Category : BaseEntity
{
    public string CategoryId { get; private set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public string? ColorCode { get; set; } // For UI theming (e.g., "#FF5733")
    public int DisplayOrder { get; set; } // For sorting in lists
    public bool IsActive { get; set; } = true;

    // Navigation property for drugs in this category
    // Not loaded by default - use explicit Include when needed

    public virtual ICollection<Drug> Drugs { get; set; } = new List<Drug>();

    public Category()
    {
        CategoryId = GenerateCategoryId();
    }

    public Category(string name, string? logoUrl = null, string? description = null, string? categoryId = null)
    {
        CategoryId = !string.IsNullOrWhiteSpace(categoryId) ? categoryId : GenerateCategoryId();
        Name = name;
        LogoUrl = logoUrl;
        Description = description;
    }

    private static string GenerateCategoryId()
    {
        return $"CAT-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}
