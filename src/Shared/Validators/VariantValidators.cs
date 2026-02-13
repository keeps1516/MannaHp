using FluentValidation;
using MannaHp.Shared.DTOs;

namespace MannaHp.Shared.Validators;

public class CreateVariantRequestValidator : AbstractValidator<CreateVariantRequest>
{
    public CreateVariantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Variant name is required")
            .MaximumLength(50);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class UpdateVariantRequestValidator : AbstractValidator<UpdateVariantRequest>
{
    public UpdateVariantRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Variant name is required")
            .MaximumLength(50);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
