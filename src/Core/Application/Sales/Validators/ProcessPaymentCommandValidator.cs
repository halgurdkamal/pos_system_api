using FluentValidation;
using pos_system_api.Core.Application.Sales.Commands.ProcessPayment;

namespace pos_system_api.Core.Application.Sales.Validators;

public class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Payment method is required")
            .Must(BeValidPaymentMethod).WithMessage("Invalid payment method. Must be: Cash, CreditCard, DebitCard, MobileMoney, BankTransfer, or Mixed");

        RuleFor(x => x.AmountPaid)
            .GreaterThan(0).WithMessage("Amount paid must be greater than 0")
            .LessThanOrEqualTo(10000000).WithMessage("Amount paid cannot exceed 10,000,000");

        RuleFor(x => x.PaymentReference)
            .MaximumLength(200).WithMessage("Payment reference cannot exceed 200 characters");
    }

    private bool BeValidPaymentMethod(string paymentMethod)
    {
        return Enum.TryParse<pos_system_api.Core.Domain.Sales.Entities.PaymentMethod>(paymentMethod, ignoreCase: true, out _);
    }
}
