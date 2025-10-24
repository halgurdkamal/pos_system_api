using FluentValidation;
using pos_system_api.Core.Application.PurchaseOrders.Commands.ReceiveStock;

namespace pos_system_api.Core.Application.PurchaseOrders.Validators;

public class ReceiveStockCommandValidator : AbstractValidator<ReceiveStockCommand>
{
    public ReceiveStockCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");

        RuleFor(x => x.ReceivedBy)
            .NotEmpty().WithMessage("Received by is required")
            .MaximumLength(100).WithMessage("Received by cannot exceed 100 characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required to receive");

        RuleForEach(x => x.Items).SetValidator(new ReceiveStockItemDtoValidator());
    }
}

public class ReceiveStockItemDtoValidator : AbstractValidator<ReceiveStockItemDto>
{
    public ReceiveStockItemDtoValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("Item ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Quantity cannot exceed 1,000,000");

        RuleFor(x => x.BatchNumber)
            .NotEmpty().WithMessage("Batch number is required")
            .MaximumLength(100).WithMessage("Batch number cannot exceed 100 characters");

        RuleFor(x => x.ExpiryDate)
            .Must(BeValidExpiryDate).WithMessage("Expiry date must be in the future");
    }

    private bool BeValidExpiryDate(DateTime expiryDate)
    {
        return expiryDate > DateTime.UtcNow;
    }
}
