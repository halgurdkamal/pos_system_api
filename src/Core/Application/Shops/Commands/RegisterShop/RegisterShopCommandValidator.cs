using FluentValidation;

namespace pos_system_api.Core.Application.Shops.Commands.RegisterShop;

/// <summary>
/// Validator for RegisterShopCommand
/// </summary>
public class RegisterShopCommandValidator : AbstractValidator<RegisterShopCommand>
{
    public RegisterShopCommandValidator()
    {
        RuleFor(x => x.ShopData)
            .NotNull()
            .WithMessage("Shop data is required");

        When(x => x.ShopData != null, () =>
        {
            // Shop Name validation
            RuleFor(x => x.ShopData.ShopName)
                .NotEmpty()
                .WithMessage("Shop name is required")
                .MaximumLength(200)
                .WithMessage("Shop name must not exceed 200 characters");

            // Legal Name validation
            RuleFor(x => x.ShopData.LegalName)
                .NotEmpty()
                .WithMessage("Legal name is required")
                .MaximumLength(200)
                .WithMessage("Legal name must not exceed 200 characters");

            // License Number validation
            RuleFor(x => x.ShopData.LicenseNumber)
                .NotEmpty()
                .WithMessage("License number is required")
                .MaximumLength(100)
                .WithMessage("License number must not exceed 100 characters");

            // Address validation
            RuleFor(x => x.ShopData.Address)
                .NotNull()
                .WithMessage("Address is required");

            When(x => x.ShopData.Address != null, () =>
            {
                RuleFor(x => x.ShopData.Address.Street)
                    .NotEmpty()
                    .WithMessage("Street address is required")
                    .MaximumLength(200)
                    .WithMessage("Street address must not exceed 200 characters");

                RuleFor(x => x.ShopData.Address.City)
                    .NotEmpty()
                    .WithMessage("City is required")
                    .MaximumLength(100)
                    .WithMessage("City must not exceed 100 characters");

                RuleFor(x => x.ShopData.Address.State)
                    .NotEmpty()
                    .WithMessage("State is required")
                    .MaximumLength(50)
                    .WithMessage("State must not exceed 50 characters");

                RuleFor(x => x.ShopData.Address.ZipCode)
                    .NotEmpty()
                    .WithMessage("Zip code is required")
                    .MaximumLength(20)
                    .WithMessage("Zip code must not exceed 20 characters");

                RuleFor(x => x.ShopData.Address.Country)
                    .NotEmpty()
                    .WithMessage("Country is required")
                    .MaximumLength(100)
                    .WithMessage("Country must not exceed 100 characters");
            });

            // Contact validation
            RuleFor(x => x.ShopData.Contact)
                .NotNull()
                .WithMessage("Contact information is required");

            When(x => x.ShopData.Contact != null, () =>
            {
                RuleFor(x => x.ShopData.Contact.Phone)
                    .NotEmpty()
                    .WithMessage("Phone number is required")
                    .Matches(@"^\+?[1-9]\d{1,14}$")
                    .WithMessage("Phone number must be in valid international format (e.g., +1-234-567-8900)");

                RuleFor(x => x.ShopData.Contact.Email)
                    .NotEmpty()
                    .WithMessage("Email is required")
                    .EmailAddress()
                    .WithMessage("Email must be a valid email address");

                RuleFor(x => x.ShopData.Contact.Website)
                    .Must(BeAValidUrl)
                    .When(x => !string.IsNullOrEmpty(x.ShopData.Contact.Website))
                    .WithMessage("Website must be a valid URL");
            });

            // Optional validations
            RuleFor(x => x.ShopData.VatRegistrationNumber)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.ShopData.VatRegistrationNumber))
                .WithMessage("VAT registration number must not exceed 50 characters");

            RuleFor(x => x.ShopData.PharmacyRegistrationNumber)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.ShopData.PharmacyRegistrationNumber))
                .WithMessage("Pharmacy registration number must not exceed 100 characters");

            RuleFor(x => x.ShopData.LogoUrl)
                .Must(BeAValidUrl)
                .When(x => !string.IsNullOrEmpty(x.ShopData.LogoUrl))
                .WithMessage("Logo URL must be a valid URL");

            RuleFor(x => x.ShopData.BrandColorPrimary)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .When(x => !string.IsNullOrEmpty(x.ShopData.BrandColorPrimary))
                .WithMessage("Primary brand color must be a valid hex color (e.g., #007BFF)");

            RuleFor(x => x.ShopData.BrandColorSecondary)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .When(x => !string.IsNullOrEmpty(x.ShopData.BrandColorSecondary))
                .WithMessage("Secondary brand color must be a valid hex color (e.g., #6C757D)");
        });
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
