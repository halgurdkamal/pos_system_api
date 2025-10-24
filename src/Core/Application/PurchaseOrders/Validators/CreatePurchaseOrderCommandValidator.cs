using FluentValidation;
using pos_system_api.Core.Application.PurchaseOrders.Commands.CreatePurchaseOrder;

namespace pos_system_api.Core.Application.PurchaseOrders.Validators;

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.ShopId)
            .NotEmpty().WithMessage("Shop ID is required")
            .MaximumLength(100).WithMessage("Shop ID cannot exceed 100 characters");

        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Supplier ID is required")
            .MaximumLength(100).WithMessage("Supplier ID cannot exceed 100 characters");

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("Created by is required")
            .MaximumLength(100).WithMessage("Created by cannot exceed 100 characters");

        RuleFor(x => x.Priority)
            .NotEmpty().WithMessage("Priority is required")
            .Must(BeValidPriority).WithMessage("Invalid priority. Must be: Low, Normal, High, or Urgent");

        RuleFor(x => x.PaymentTerms)
            .NotEmpty().WithMessage("Payment terms are required")
            .Must(BeValidPaymentTerms).WithMessage("Invalid payment terms. Must be: Immediate, Net15, Net30, Net45, Net60, or Custom");

        RuleFor(x => x.CustomPaymentTerms)
            .MaximumLength(200).WithMessage("Custom payment terms cannot exceed 200 characters")
            .NotEmpty().When(x => x.PaymentTerms.Equals("Custom", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Custom payment terms are required when payment terms is 'Custom'");

        RuleFor(x => x.ExpectedDeliveryDate)
            .Must(BeValidDeliveryDate).When(x => x.ExpectedDeliveryDate.HasValue)
            .WithMessage("Expected delivery date must be in the future");

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes cannot exceed 2000 characters");

        RuleFor(x => x.DeliveryAddress)
            .MaximumLength(500).WithMessage("Delivery address cannot exceed 500 characters");

        RuleFor(x => x.ReferenceNumber)
            .MaximumLength(100).WithMessage("Reference number cannot exceed 100 characters");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required")
            .Must(items => items.Count <= 1000).WithMessage("Cannot have more than 1000 items in one order");

        RuleForEach(x => x.Items).SetValidator(new CreatePurchaseOrderItemDtoValidator());

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0).When(x => x.ShippingCost.HasValue)
            .WithMessage("Shipping cost must be non-negative");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).When(x => x.TaxAmount.HasValue)
            .WithMessage("Tax amount must be non-negative");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).When(x => x.DiscountAmount.HasValue)
            .WithMessage("Discount amount must be non-negative");
    }

    private bool BeValidPriority(string priority)
    {
        return Enum.TryParse<pos_system_api.Core.Domain.PurchaseOrders.Entities.OrderPriority>(priority, ignoreCase: true, out _);
    }

    private bool BeValidPaymentTerms(string paymentTerms)
    {
        return Enum.TryParse<pos_system_api.Core.Domain.PurchaseOrders.Entities.PaymentTerms>(paymentTerms, ignoreCase: true, out _);
    }

    private bool BeValidDeliveryDate(DateTime? deliveryDate)
    {
        return deliveryDate == null || deliveryDate.Value > DateTime.UtcNow;
    }
}

public class CreatePurchaseOrderItemDtoValidator : AbstractValidator<CreatePurchaseOrderItemDto>
{
    public CreatePurchaseOrderItemDtoValidator()
    {
        RuleFor(x => x.DrugId)
            .NotEmpty().WithMessage("Drug ID is required")
            .MaximumLength(100).WithMessage("Drug ID cannot exceed 100 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Quantity cannot exceed 1,000,000");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("Unit price must be greater than 0")
            .LessThanOrEqualTo(1000000).WithMessage("Unit price cannot exceed 1,000,000");

        RuleFor(x => x.DiscountPercentage)
            .GreaterThanOrEqualTo(0).When(x => x.DiscountPercentage.HasValue)
            .WithMessage("Discount percentage must be non-negative")
            .LessThanOrEqualTo(100).When(x => x.DiscountPercentage.HasValue)
            .WithMessage("Discount percentage cannot exceed 100%");
    }
}
