using pos_system_api.Core.Domain.Categories.Entities;

namespace pos_system_api.Infrastructure.SampleData;

public static class CategorySeedData
{
    public static List<Category> GetCategories()
    {
        return new List<Category>
        {
            new Category("Cardiovascular",
                "https://i.ibb.co/p6dGvWqH/037-Heart-1.png",
                "Medications for heart and blood pressure conditions",
                "CAT-F558660D")
            {
                ColorCode = "#FF6B6B",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.Parse("2025-10-29T21:14:16.945946Z").ToUniversalTime(),
                CreatedBy = "system"
            },
            new Category("Pain Relief",
                "https://i.ibb.co/HDR2cznk/035-Tablet-Pack.png",
                "Analgesics and pain management medications",
                "CAT-45877BC0")
            {
                ColorCode = "#4ECDC4",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.Parse("2025-10-29T21:16:59.762209Z").ToUniversalTime(),
                CreatedBy = "system"
            },
            new Category("Respiratory",
                "https://i.ibb.co/rKyDHxHr/024-Lungs.png",
                "Medications for breathing and lung conditions",
                "CAT-C3A40592")
            {
                ColorCode = "#45B7D1",
                DisplayOrder = 3,
                IsActive = true,
                CreatedAt = DateTime.Parse("2025-10-29T21:18:17.264338Z").ToUniversalTime(),
                CreatedBy = "system"
            },
            new Category("Diabetes",
                "https://i.ibb.co/236rH1SJ/015-Blood-Drops.png",
                "Medications for blood sugar management",
                "CAT-B770BCA7")
            {
                ColorCode = "#FFEAA7",
                DisplayOrder = 5,
                IsActive = true,
                CreatedAt = DateTime.Parse("2025-10-29T21:19:50.388372Z").ToUniversalTime(),
                CreatedBy = "system"
            }
        };
    }
}
