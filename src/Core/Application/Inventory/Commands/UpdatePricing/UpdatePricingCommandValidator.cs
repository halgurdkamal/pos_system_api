using FluentValidation;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdatePricing;

/// <summary>
/// Validator for UpdatePricingCommand
/// </summary>
public class UpdatePricingCommandValidator : AbstractValidator<UpdatePricingCommand>
{
    public UpdatePricingCommandValidator()
    {
        RuleFor(x => x.ShopId)
            .NotEmpty()
            .WithMessage("Shop ID is required")
            .MaximumLength(50)
            .WithMessage("Shop ID must not exceed 50 characters");

        RuleFor(x => x.DrugId)
            .NotEmpty()
            .WithMessage("Drug ID is required")
            .MaximumLength(50)
            .WithMessage("Drug ID must not exceed 50 characters");

        RuleFor(x => x.CostPrice)
            .GreaterThan(0)
            .WithMessage("Cost price must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Cost price must not exceed 1,000,000");

        RuleFor(x => x.SellingPrice)
            .GreaterThan(0)
            .WithMessage("Selling price must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Selling price must not exceed 1,000,000")
            .GreaterThanOrEqualTo(x => x.CostPrice)
            .WithMessage("Selling price must be greater than or equal to cost price");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 100)
            .When(x => x.TaxRate.HasValue)
            .WithMessage("Tax rate must be between 0 and 100");
    }
}
