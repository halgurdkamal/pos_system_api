using Microsoft.Extensions.Logging.Abstractions;
using POS.Tests.Unit.Infrastructure;
using pos_system_api.Core.Application.Common.Exceptions;
using pos_system_api.Core.Application.Drugs.Commands.CreateDrug;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Domain.Categories.Entities;
using pos_system_api.Core.Domain.Drugs.ValueObjects;
using pos_system_api.Infrastructure.Data;
using pos_system_api.Infrastructure.Data.Repositories;

namespace POS.Tests.Unit.Application.Drugs;

public class CreateDrugCommandHandlerTests
{
    [Fact]
    public async Task Handle_HappyPath_PersistsDrugAndReturnsDto()
    {
        using var db = TestDbContextFactory.Create();
        await SeedCategoryAsync(db, categoryId: "CAT-PAIN", name: "Pain Relief");

        var handler = NewHandler(db);
        var command = new CreateDrugCommand(
            new CreateDrugDto
            {
                DrugId = "DRG-PARACETAMOL",
                Barcode = "1234567890123",
                BrandName = "Panadol",
                GenericName = "Paracetamol",
                CategoryId = "CAT-PAIN",
                PackagingInfo = MinimalPackagingInfo(),
            },
            CreatedBy: "user-1");

        var result = await handler.Handle(command, CancellationToken.None);

        result.DrugId.Should().Be("DRG-PARACETAMOL");
        result.BrandName.Should().Be("Panadol");
        result.GenericName.Should().Be("Paracetamol");
        result.CategoryId.Should().Be("CAT-PAIN");
        result.Category.Should().Be("Pain Relief");
        result.CreatedBy.Should().Be("user-1");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify it was actually persisted
        var persisted = db.Drugs.FirstOrDefault(d => d.DrugId == "DRG-PARACETAMOL");
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_AutoGeneratesDrugId_WhenNotProvided()
    {
        using var db = TestDbContextFactory.Create();
        await SeedCategoryAsync(db, "CAT-A", "Cat A");

        var handler = NewHandler(db);
        var command = new CreateDrugCommand(
            new CreateDrugDto
            {
                BrandName = "B",
                GenericName = "G",
                CategoryId = "CAT-A",
                PackagingInfo = MinimalPackagingInfo(),
            });

        var result = await handler.Handle(command, CancellationToken.None);

        result.DrugId.Should().StartWith("DRG-");
        result.DrugId.Length.Should().Be(12);
    }

    [Fact]
    public async Task Handle_DefaultsCreatedByToSystem_WhenEmptyOrWhitespace()
    {
        using var db = TestDbContextFactory.Create();
        await SeedCategoryAsync(db, "CAT-A", "Cat A");

        var handler = NewHandler(db);
        var command = new CreateDrugCommand(
            new CreateDrugDto
            {
                BrandName = "B",
                GenericName = "G",
                CategoryId = "CAT-A",
                PackagingInfo = MinimalPackagingInfo(),
            },
            CreatedBy: "   ");

        var result = await handler.Handle(command, CancellationToken.None);

        result.CreatedBy.Should().Be("system");
    }

    [Fact]
    public async Task Handle_DuplicateDrugId_ThrowsConflictException()
    {
        using var db = TestDbContextFactory.Create();
        await SeedCategoryAsync(db, "CAT-A", "Cat A");

        var handler = NewHandler(db);
        var first = new CreateDrugCommand(
            new CreateDrugDto
            {
                DrugId = "DRG-DUPE",
                BrandName = "B",
                GenericName = "G",
                CategoryId = "CAT-A",
                PackagingInfo = MinimalPackagingInfo(),
            });
        await handler.Handle(first, CancellationToken.None);

        var second = new CreateDrugCommand(
            new CreateDrugDto
            {
                DrugId = "DRG-DUPE",
                BrandName = "Other",
                GenericName = "Other",
                CategoryId = "CAT-A",
                PackagingInfo = MinimalPackagingInfo(),
            });

        var act = () => handler.Handle(second, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*DRG-DUPE*already exists*");
    }

    [Fact]
    public async Task Handle_DuplicateBarcode_ThrowsConflictException()
    {
        using var db = TestDbContextFactory.Create();
        await SeedCategoryAsync(db, "CAT-A", "Cat A");

        var handler = NewHandler(db);
        var first = new CreateDrugCommand(
            new CreateDrugDto
            {
                Barcode = "BAR-99",
                BrandName = "B1",
                GenericName = "G1",
                CategoryId = "CAT-A",
                PackagingInfo = MinimalPackagingInfo(),
            });
        await handler.Handle(first, CancellationToken.None);

        var second = new CreateDrugCommand(
            new CreateDrugDto
            {
                Barcode = "BAR-99",
                BrandName = "B2",
                GenericName = "G2",
                CategoryId = "CAT-A",
                PackagingInfo = MinimalPackagingInfo(),
            });

        var act = () => handler.Handle(second, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*BAR-99*already assigned*");
    }

    [Fact]
    public async Task Handle_MissingCategory_ThrowsNotFoundException()
    {
        using var db = TestDbContextFactory.Create();
        // No category seeded.

        var handler = NewHandler(db);
        var command = new CreateDrugCommand(
            new CreateDrugDto
            {
                BrandName = "B",
                GenericName = "G",
                CategoryId = "CAT-MISSING",
                PackagingInfo = MinimalPackagingInfo(),
            });

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*CAT-MISSING*does not exist*");
    }

    // ----- helpers -----

    private static CreateDrugCommandHandler NewHandler(ApplicationDbContext db) =>
        new(
            new DrugRepository(db),
            new CategoryRepository(db),
            NullLogger<CreateDrugCommandHandler>.Instance);

    private static async Task SeedCategoryAsync(
        ApplicationDbContext db, string categoryId, string name)
    {
        db.Categories.Add(new Category(name: name, categoryId: categoryId));
        await db.SaveChangesAsync();
    }

    private static CreatePackagingInfoDto MinimalPackagingInfo() => new()
    {
        UnitType = UnitType.Count,
        BaseUnit = "tablet",
        BaseUnitDisplayName = "Tablet",
        IsSubdivisible = true,
        PackagingLevels = new List<CreatePackagingLevelDto>
        {
            new()
            {
                LevelNumber = 1,
                UnitName = "Tablet",
                BaseUnitQuantity = 1m,
                IsSellable = true,
                IsDefault = true,
                IsBreakable = false,
                MinimumSaleQuantity = 1m,
            },
        },
    };
}
