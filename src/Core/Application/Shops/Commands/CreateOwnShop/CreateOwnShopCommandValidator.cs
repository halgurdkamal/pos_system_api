using FluentValidation;

namespace pos_system_api.Core.Application.Shops.Commands.CreateOwnShop;

public class CreateOwnShopCommandValidator : AbstractValidator<CreateOwnShopCommand>
{
    public CreateOwnShopCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.ShopName)
            .NotEmpty().WithMessage("Shop name is required")
            .MinimumLength(3).WithMessage("Shop name must be at least 3 characters")
            .MaximumLength(100).WithMessage("Shop name must not exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Phone number must be a valid international format");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required")
            .MinimumLength(5).WithMessage("Address must be at least 5 characters");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required");

        RuleFor(x => x.LicenseNumber)
            .MinimumLength(5).When(x => !string.IsNullOrEmpty(x.LicenseNumber))
            .WithMessage("License number must be at least 5 characters if provided");

        RuleFor(x => x.Description)
            .MaximumLength(100).When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 100 characters");
    }
}
