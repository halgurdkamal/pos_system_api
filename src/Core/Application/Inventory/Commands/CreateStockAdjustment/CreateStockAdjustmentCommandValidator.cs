using FluentValidation;

namespace pos_system_api.Core.Application.Inventory.Commands.CreateStockAdjustment;

public class CreateStockAdjustmentCommandValidator : AbstractValidator<CreateStockAdjustmentCommand>
{
    public CreateStockAdjustmentCommandValidator()
    {
        RuleFor(x => x.ShopId)
            .NotEmpty()
            .WithMessage("Shop ID is required");

        RuleFor(x => x.DrugId)
            .NotEmpty()
            .WithMessage("Drug ID is required");

        RuleFor(x => x.AdjustmentType)
            .NotEmpty()
            .WithMessage("Adjustment type is required")
            .Must(BeValidAdjustmentType)
            .WithMessage("Invalid adjustment type. Valid types: Sale, Return, Damage, Expired, Theft, Correction, TransferOut, TransferIn, Receipt, LocationMove, Recall");

        RuleFor(x => x.QuantityChanged)
            .NotEqual(0)
            .WithMessage("Quantity changed cannot be zero");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes cannot exceed 1000 characters");

        RuleFor(x => x.AdjustedBy)
            .NotEmpty()
            .WithMessage("Adjusted by (User ID) is required");
    }

    private bool BeValidAdjustmentType(string type)
    {
        return Enum.TryParse<pos_system_api.Core.Domain.Inventory.Entities.AdjustmentType>(type, true, out _);
    }
}
