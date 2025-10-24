using FluentValidation;
using pos_system_api.Core.Application.Sales.Commands.CreateSalesOrder;

namespace pos_system_api.Core.Application.Sales.Validators;

public class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.ShopId)
            .NotEmpty().WithMessage("Shop ID is required")
            .MaximumLength(100).WithMessage("Shop ID cannot exceed 100 characters");

        RuleFor(x => x.CashierId)
            .NotEmpty().WithMessage("Cashier ID is required")
            .MaximumLength(100).WithMessage("Cashier ID cannot exceed 100 characters");

        RuleFor(x => x.CustomerId)
            .MaximumLength(100).WithMessage("Customer ID cannot exceed 100 characters");

        RuleFor(x => x.CustomerName)
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters");

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(50).WithMessage("Customer phone cannot exceed 50 characters");

        RuleFor(x => x.PrescriptionNumber)
            .NotEmpty().When(x => x.IsPrescriptionRequired)
            .WithMessage("Prescription number is required when prescription is required")
            .MaximumLength(100).WithMessage("Prescription number cannot exceed 100 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required")
            .Must(items => items.Count <= 1000).WithMessage("Cannot have more than 1000 items in one order");

        RuleForEach(x => x.Items).SetValidator(new CreateSalesOrderItemDtoValidator());

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).When(x => x.TaxAmount.HasValue)
            .WithMessage("Tax amount must be non-negative");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue)
            .WithMessage("Discount amount must be non-negative");
    }
}

public class CreateSalesOrderItemDtoValidator : AbstractValidator<CreateSalesOrderItemDto>
{
    public CreateSalesOrderItemDtoValidator()
    {
        RuleFor(x => x.DrugId)
            .NotEmpty().WithMessage("Drug ID is required")
            .MaximumLength(100).WithMessage("Drug ID cannot exceed 100 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(10000).WithMessage("Quantity cannot exceed 10,000");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Unit price cannot exceed 1,000,000");

        RuleFor(x => x.DiscountPercentage)
            .GreaterThanOrEqualTo(0).When(x => x.DiscountPercentage.HasValue)
            .WithMessage("Discount percentage must be non-negative")
            .LessThanOrEqualTo(100).When(x => x.DiscountPercentage.HasValue)
            .WithMessage("Discount percentage cannot exceed 100%");

        RuleFor(x => x.BatchNumber)
            .MaximumLength(100).WithMessage("Batch number cannot exceed 100 characters");
    }
}
