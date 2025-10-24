using pos_system_api.Core.Domain.Categories.Entities;

namespace pos_system_api.Infrastructure.SampleData;

public static class CategorySeedData
{
    public static List<Category> GetCategories()
    {
        return new List<Category>
        {
            new Category("Pain Relief", "https://cdn-icons-png.flaticon.com/512/3588/3588435.png", "Medications for pain management")
            {
                ColorCode = "#FF6B6B",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Antibiotics", "https://cdn-icons-png.flaticon.com/512/2913/2913133.png", "Antibacterial medications")
            {
                ColorCode = "#4ECDC4",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Vitamins & Supplements", "https://cdn-icons-png.flaticon.com/512/2553/2553644.png", "Nutritional supplements and vitamins")
            {
                ColorCode = "#95E1D3",
                DisplayOrder = 3,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Cold & Flu", "https://cdn-icons-png.flaticon.com/512/3588/3588592.png", "Cold and flu treatments")
            {
                ColorCode = "#A8E6CF",
                DisplayOrder = 4,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Digestive Health", "https://cdn-icons-png.flaticon.com/512/2913/2913179.png", "Digestive and gastrointestinal medications")
            {
                ColorCode = "#FFD93D",
                DisplayOrder = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Heart & Blood Pressure", "https://cdn-icons-png.flaticon.com/512/2913/2913133.png", "Cardiovascular medications")
            {
                ColorCode = "#F38181",
                DisplayOrder = 6,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Diabetes Care", "https://cdn-icons-png.flaticon.com/512/3588/3588569.png", "Diabetes management medications")
            {
                ColorCode = "#AA96DA",
                DisplayOrder = 7,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Skin Care", "https://cdn-icons-png.flaticon.com/512/2913/2913146.png", "Dermatological products")
            {
                ColorCode = "#FCBAD3",
                DisplayOrder = 8,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Eye & Ear Care", "https://cdn-icons-png.flaticon.com/512/2913/2913185.png", "Ophthalmic and otic medications")
            {
                ColorCode = "#FFFFD2",
                DisplayOrder = 9,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Allergy & Asthma", "https://cdn-icons-png.flaticon.com/512/3588/3588525.png", "Allergy and respiratory medications")
            {
                ColorCode = "#A0C4FF",
                DisplayOrder = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Baby & Child Care", "https://cdn-icons-png.flaticon.com/512/2913/2913098.png", "Pediatric medications")
            {
                ColorCode = "#BDB2FF",
                DisplayOrder = 11,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Women's Health", "https://cdn-icons-png.flaticon.com/512/3588/3588521.png", "Women's health products")
            {
                ColorCode = "#FFC6FF",
                DisplayOrder = 12,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("First Aid", "https://cdn-icons-png.flaticon.com/512/2913/2913061.png", "First aid supplies")
            {
                ColorCode = "#CAFFBF",
                DisplayOrder = 13,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Mental Health", "https://cdn-icons-png.flaticon.com/512/3588/3588604.png", "Psychiatric medications")
            {
                ColorCode = "#9BF6FF",
                DisplayOrder = 14,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new Category("Other", "https://cdn-icons-png.flaticon.com/512/2913/2913133.png", "Other medications")
            {
                ColorCode = "#E0E0E0",
                DisplayOrder = 99,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        };
    }
}
