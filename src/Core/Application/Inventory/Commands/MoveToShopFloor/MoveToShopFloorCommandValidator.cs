using FluentValidation;

namespace pos_system_api.Core.Application.Inventory.Commands.MoveToShopFloor;

public class MoveToShopFloorCommandValidator : AbstractValidator<MoveToShopFloorCommand>
{
    public MoveToShopFloorCommandValidator()
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
