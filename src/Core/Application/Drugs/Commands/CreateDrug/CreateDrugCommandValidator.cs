using FluentValidation;

namespace pos_system_api.Core.Application.Drugs.Commands.CreateDrug;

public class CreateDrugCommandValidator : AbstractValidator<CreateDrugCommand>
{
    public CreateDrugCommandValidator()
    {
        RuleFor(x => x.Payload)
            .NotNull()
            .WithMessage("Drug payload is required.");

        When(x => x.Payload != null, () =>
        {
            RuleFor(x => x.Payload.BrandName)
                .NotEmpty()
                .WithMessage("BrandName is required.");

            RuleFor(x => x.Payload.GenericName)
                .NotEmpty()
                .WithMessage("GenericName is required.");

            RuleFor(x => x.Payload.CategoryId)
                .NotEmpty()
                .WithMessage("CategoryId is required.");

            RuleFor(x => x.Payload.PackagingInfo)
                .NotNull()
                .WithMessage("PackagingInfo is required.");

            When(x => x.Payload.PackagingInfo != null, () =>
            {
                RuleFor(x => x.Payload.PackagingInfo.PackagingLevels)
                    .NotEmpty()
                    .WithMessage("PackagingLevels must contain at least one level.");
            });
        });
    }
}
