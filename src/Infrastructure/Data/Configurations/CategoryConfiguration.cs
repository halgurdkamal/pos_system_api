using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using pos_system_api.Core.Domain.Categories.Entities;

namespace pos_system_api.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CategoryId)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.CategoryId)
            .IsUnique();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.Name)
            .IsUnique();

        builder.Property(c => c.LogoUrl)
            .HasMaxLength(500);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.ColorCode)
            .HasMaxLength(7); // #FFFFFF format

        builder.Property(c => c.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100);

        builder.Property(c => c.LastUpdated);

        builder.Property(c => c.UpdatedBy)
            .HasMaxLength(100);
    }
}
