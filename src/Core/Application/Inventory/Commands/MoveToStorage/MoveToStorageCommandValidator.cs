using FluentValidation;

namespace pos_system_api.Core.Application.Inventory.Commands.MoveToStorage;

public class MoveToStorageCommandValidator : AbstractValidator<MoveToStorageCommand>
{
    public MoveToStorageCommandValidator()
    {
        RuleFor(x => x.ShopId)
            .NotEmpty()
            .WithMessage("Shop ID is required");

        RuleFor(x => x.DrugId)
            .NotEmpty()
            .WithMessage("Drug ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");
    }
}
