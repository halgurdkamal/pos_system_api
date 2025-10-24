using FluentValidation;

namespace pos_system_api.Core.Application.ShopUsers.Commands.AddUserToShop;

public class AddUserToShopCommandValidator : AbstractValidator<AddUserToShopCommand>
{
    public AddUserToShopCommandValidator()
    {
        RuleFor(x => x.ShopId)
            .NotEmpty().WithMessage("Shop ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .Must(BeValidRole).WithMessage("Invalid role. Valid roles: Owner, Manager, Cashier, InventoryClerk, Viewer, Custom");

        RuleFor(x => x.InvitedBy)
            .NotEmpty().WithMessage("InvitedBy (current user ID) is required");
    }

    private bool BeValidRole(string role)
    {
        return role switch
        {
            "Owner" or "Manager" or "Cashier" or "InventoryClerk" or "Viewer" or "Custom" => true,
            _ => false
        };
    }
}
