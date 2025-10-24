using FluentValidation;

namespace pos_system_api.Core.Application.Inventory.Commands.AddStock;

/// <summary>
/// Validator for AddStockCommand
/// </summary>
public class AddStockCommandValidator : AbstractValidator<AddStockCommand>
{
    public AddStockCommandValidator()
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

        RuleFor(x => x.BatchNumber)
            .NotEmpty()
            .WithMessage("Batch number is required")
            .MaximumLength(100)
            .WithMessage("Batch number must not exceed 100 characters");

        RuleFor(x => x.SupplierId)
            .NotEmpty()
            .WithMessage("Supplier ID is required")
            .MaximumLength(50)
            .WithMessage("Supplier ID must not exceed 50 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Quantity must not exceed 1,000,000");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Expiry date must be in the future");

        RuleFor(x => x.PurchasePrice)
            .GreaterThan(0)
            .WithMessage("Purchase price must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Purchase price must not exceed 1,000,000");

        RuleFor(x => x.SellingPrice)
            .GreaterThan(0)
            .WithMessage("Selling price must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Selling price must not exceed 1,000,000")
            .GreaterThanOrEqualTo(x => x.PurchasePrice)
            .WithMessage("Selling price must be greater than or equal to purchase price");

        RuleFor(x => x.StorageLocation)
            .NotEmpty()
            .WithMessage("Storage location is required")
            .MaximumLength(100)
            .WithMessage("Storage location must not exceed 100 characters");

        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ReorderPoint.HasValue)
            .WithMessage("Reorder point must be non-negative");
    }
}
