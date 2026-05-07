using pos_system_api.Core.Application.Common.DTOs;
using pos_system_api.Core.Application.Suppliers.Commands.CreateSupplier;
using pos_system_api.Core.Application.Suppliers.DTOs;

namespace POS.Tests.Unit.Application.Suppliers;

public class CreateSupplierCommandValidatorTests
{
    private readonly CreateSupplierCommandValidator _validator = new();

    private static CreateSupplierDto ValidDto() => new()
    {
        SupplierName = "Acme Pharma",
        SupplierType = "Distributor",
        ContactNumber = "+12025550100",
        Email = "contact@acme.test",
        Address = new AddressDto
        {
            Street = "123 Main St",
            City = "Springfield",
            State = "IL",
            ZipCode = "62701",
            Country = "USA",
        },
        PaymentTerms = "Net 30",
        DeliveryLeadTime = 7,
        MinimumOrderValue = 100,
    };

    private static CreateSupplierCommand WithDto(CreateSupplierDto dto) =>
        new("Acme", "Distributor", dto.ContactNumber, dto.Email, dto);

    [Fact]
    public void Valid_PassesValidation()
    {
        var result = _validator.Validate(WithDto(ValidDto()));

        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void MissingSupplierData_Fails()
    {
        var cmd = new CreateSupplierCommand("Acme", "Distributor", "+12025550100", "x@y.com", null!);

        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Supplier data is required");
    }

    [Fact]
    public void EmptySupplierName_Fails()
    {
        var dto = ValidDto();
        dto.SupplierName = "";

        var result = _validator.Validate(WithDto(dto));

        result.Errors.Should().Contain(e => e.ErrorMessage == "Supplier name is required");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void InvalidEmail_Fails(string email)
    {
        var dto = ValidDto();
        dto.Email = email;

        var result = _validator.Validate(WithDto(dto));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("+12025550100")]
    [InlineData("9647712345678")]
    public void ValidContactNumber_Passes(string number)
    {
        var dto = ValidDto();
        dto.ContactNumber = number;

        var result = _validator.Validate(WithDto(dto));

        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0123456")]   // leading zero not allowed by ^\+?[1-9]
    [InlineData("+0123456")]  // leading zero after + not allowed
    public void InvalidContactNumber_Fails(string number)
    {
        var dto = ValidDto();
        dto.ContactNumber = number;

        var result = _validator.Validate(WithDto(dto));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("international format"));
    }

    [Theory]
    [InlineData("Manufacturer")]
    [InlineData("Distributor")]
    [InlineData("Wholesaler")]
    [InlineData("LocalAgent")]
    [InlineData("manufacturer")] // case-insensitive enum parse
    public void ValidSupplierType_Passes(string supplierType)
    {
        var dto = ValidDto();
        dto.SupplierType = supplierType;

        var result = _validator.Validate(WithDto(dto));

        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void UnknownSupplierType_Fails()
    {
        var dto = ValidDto();
        dto.SupplierType = "Bandit";

        var result = _validator.Validate(WithDto(dto));

        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Invalid supplier type"));
    }

    [Fact]
    public void DeliveryLeadTime_MustBePositive()
    {
        var dto = ValidDto();
        dto.DeliveryLeadTime = 0;

        var result = _validator.Validate(WithDto(dto));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DeliveryLeadTime_AboveYear_Fails()
    {
        var dto = ValidDto();
        dto.DeliveryLeadTime = 400;

        var result = _validator.Validate(WithDto(dto));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeMinimumOrderValue_Fails()
    {
        var dto = ValidDto();
        dto.MinimumOrderValue = -1;

        var result = _validator.Validate(WithDto(dto));

        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("non-negative"));
    }

    [Theory]
    [InlineData("https://acme.test")]
    [InlineData("http://acme.test/path")]
    public void ValidWebsiteUrl_Passes(string url)
    {
        var dto = ValidDto();
        dto.Website = url;

        var result = _validator.Validate(WithDto(dto));

        result.IsValid.Should().BeTrue(string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Theory]
    [InlineData("acme.test")]      // missing scheme
    [InlineData("ftp://acme.test")] // wrong scheme
    public void InvalidWebsiteUrl_Fails(string url)
    {
        var dto = ValidDto();
        dto.Website = url;

        var result = _validator.Validate(WithDto(dto));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyAddressCountry_Fails()
    {
        var dto = ValidDto();
        dto.Address.Country = "";

        var result = _validator.Validate(WithDto(dto));

        result.Errors.Should().Contain(e => e.ErrorMessage == "Country is required");
    }
}
