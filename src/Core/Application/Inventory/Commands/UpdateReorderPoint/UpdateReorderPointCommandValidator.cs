using FluentValidation;

namespace pos_system_api.Core.Application.Inventory.Commands.UpdateReorderPoint;

/// <summary>
/// Validator for UpdateReorderPointCommand
/// </summary>
public class UpdateReorderPointCommandValidator : AbstractValidator<UpdateReorderPointCommand>
{
    public UpdateReorderPointCommandValidator()
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

        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Reorder point must be non-negative")
            .LessThanOrEqualTo(10000)
            .WithMessage("Reorder point must not exceed 10,000");
    }
}
