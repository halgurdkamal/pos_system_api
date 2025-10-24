using FluentValidation;

namespace pos_system_api.Core.Application.Suppliers.Commands.CreateSupplier;

/// <summary>
/// Validator for CreateSupplierCommand
/// </summary>
public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.SupplierData)
            .NotNull()
            .WithMessage("Supplier data is required");

        When(x => x.SupplierData != null, () =>
        {
            // Supplier Name validation
            RuleFor(x => x.SupplierData.SupplierName)
                .NotEmpty()
                .WithMessage("Supplier name is required")
                .MaximumLength(200)
                .WithMessage("Supplier name must not exceed 200 characters");

            // Email validation
            RuleFor(x => x.SupplierData.Email)
                .NotEmpty()
                .WithMessage("Email is required")
                .EmailAddress()
                .WithMessage("Email must be a valid email address");

            // Contact Number validation
            RuleFor(x => x.SupplierData.ContactNumber)
                .NotEmpty()
                .WithMessage("Contact number is required")
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Contact number must be in valid international format");

            // Supplier Type validation
            RuleFor(x => x.SupplierData.SupplierType)
                .IsInEnum()
                .WithMessage("Invalid supplier type");

            // Payment Terms validation
            RuleFor(x => x.SupplierData.PaymentTerms)
                .NotEmpty()
                .WithMessage("Payment terms are required")
                .MaximumLength(100)
                .WithMessage("Payment terms must not exceed 100 characters");

            // Delivery Lead Time validation
            RuleFor(x => x.SupplierData.DeliveryLeadTime)
                .GreaterThan(0)
                .WithMessage("Delivery lead time must be greater than 0 days")
                .LessThanOrEqualTo(365)
                .WithMessage("Delivery lead time must not exceed 365 days");

            // Minimum Order Value validation
            RuleFor(x => x.SupplierData.MinimumOrderValue)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum order value must be non-negative");

            // Address validation
            RuleFor(x => x.SupplierData.Address)
                .NotNull()
                .WithMessage("Address is required");

            When(x => x.SupplierData.Address != null, () =>
            {
                RuleFor(x => x.SupplierData.Address.Street)
                    .NotEmpty()
                    .WithMessage("Street address is required")
                    .MaximumLength(200)
                    .WithMessage("Street address must not exceed 200 characters");

                RuleFor(x => x.SupplierData.Address.City)
                    .NotEmpty()
                    .WithMessage("City is required")
                    .MaximumLength(100)
                    .WithMessage("City must not exceed 100 characters");

                RuleFor(x => x.SupplierData.Address.State)
                    .MaximumLength(50)
                    .WithMessage("State must not exceed 50 characters");

                RuleFor(x => x.SupplierData.Address.ZipCode)
                    .MaximumLength(20)
                    .WithMessage("Zip code must not exceed 20 characters");

                RuleFor(x => x.SupplierData.Address.Country)
                    .NotEmpty()
                    .WithMessage("Country is required")
                    .MaximumLength(100)
                    .WithMessage("Country must not exceed 100 characters");
            });

            // Optional validations
            RuleFor(x => x.SupplierData.Website)
                .Must(BeAValidUrl)
                .When(x => !string.IsNullOrEmpty(x.SupplierData.Website))
                .WithMessage("Website must be a valid URL");

            RuleFor(x => x.SupplierData.LicenseNumber)
                .MaximumLength(100)
                .When(x => !string.IsNullOrEmpty(x.SupplierData.LicenseNumber))
                .WithMessage("License number must not exceed 100 characters");

            RuleFor(x => x.SupplierData.TaxId)
                .MaximumLength(50)
                .When(x => !string.IsNullOrEmpty(x.SupplierData.TaxId))
                .WithMessage("Tax ID must not exceed 50 characters");
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
