using FluentValidation;

namespace pos_system_api.Core.Application.Inventory.Commands.ReduceStock;

/// <summary>
/// Validator for ReduceStockCommand
/// </summary>
public class ReduceStockCommandValidator : AbstractValidator<ReduceStockCommand>
{
    public ReduceStockCommandValidator()
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

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(100000)
            .WithMessage("Quantity must not exceed 100,000 per transaction");
    }
}
