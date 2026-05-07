using pos_system_api.Core.Application.Drugs.Commands.CreateDrug;
using pos_system_api.Core.Application.Drugs.DTOs;
using pos_system_api.Core.Domain.Drugs.ValueObjects;

namespace POS.Tests.Unit.Application.Drugs;

public class CreateDrugCommandValidatorTests
{
    private readonly CreateDrugCommandValidator _validator = new();

    [Fact]
    public void Valid_PassesValidation()
    {
        var result = _validator.Validate(new CreateDrugCommand(ValidDto()));

        result.IsValid.Should().BeTrue(
            string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void NullPayload_Fails()
    {
        var result = _validator.Validate(new CreateDrugCommand(null!));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Drug payload is required.");
    }

    [Fact]
    public void EmptyBrandName_Fails()
    {
        var dto = ValidDto();
        dto.BrandName = "";

        var result = _validator.Validate(new CreateDrugCommand(dto));

        result.Errors.Should().Contain(e => e.ErrorMessage == "BrandName is required.");
    }

    [Fact]
    public void EmptyGenericName_Fails()
    {
        var dto = ValidDto();
        dto.GenericName = "";

        var result = _validator.Validate(new CreateDrugCommand(dto));

        result.Errors.Should().Contain(e => e.ErrorMessage == "GenericName is required.");
    }

    [Fact]
    public void EmptyCategoryId_Fails()
    {
        var dto = ValidDto();
        dto.CategoryId = "";

        var result = _validator.Validate(new CreateDrugCommand(dto));

        result.Errors.Should().Contain(e => e.ErrorMessage == "CategoryId is required.");
    }

    [Fact]
    public void NullPackagingInfo_Fails()
    {
        var dto = ValidDto();
        dto.PackagingInfo = null!;

        var result = _validator.Validate(new CreateDrugCommand(dto));

        result.Errors.Should().Contain(e => e.ErrorMessage == "PackagingInfo is required.");
    }

    [Fact]
    public void EmptyPackagingLevels_Fails()
    {
        var dto = ValidDto();
        dto.PackagingInfo.PackagingLevels.Clear();

        var result = _validator.Validate(new CreateDrugCommand(dto));

        result.Errors.Should().Contain(e =>
            e.ErrorMessage == "PackagingLevels must contain at least one level.");
    }

    private static CreateDrugDto ValidDto() => new()
    {
        BrandName = "Panadol",
        GenericName = "Paracetamol",
        CategoryId = "CAT-PAIN",
        PackagingInfo = new CreatePackagingInfoDto
        {
            UnitType = UnitType.Count,
            BaseUnit = "tablet",
            BaseUnitDisplayName = "Tablet",
            PackagingLevels = new List<CreatePackagingLevelDto>
            {
                new()
                {
                    LevelNumber = 1,
                    UnitName = "Tablet",
                    BaseUnitQuantity = 1m,
                    IsSellable = true,
                    IsDefault = true,
                },
            },
        },
    };
}
