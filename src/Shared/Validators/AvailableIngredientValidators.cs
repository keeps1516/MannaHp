using FluentValidation;
using MannaHp.Shared.DTOs;

namespace MannaHp.Shared.Validators;

public class CreateAvailableIngredientRequestValidator : AbstractValidator<CreateAvailableIngredientRequest>
{
    public CreateAvailableIngredientRequestValidator()
    {
        RuleFor(x => x.IngredientId)
            .NotEmpty().WithMessage("Ingredient is required");

        RuleFor(x => x.CustomerPrice)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.QuantityUsed)
            .GreaterThan(0).WithMessage("Quantity used must be greater than zero");

        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Group name is required")
            .MaximumLength(50);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class UpdateAvailableIngredientRequestValidator : AbstractValidator<UpdateAvailableIngredientRequest>
{
    public UpdateAvailableIngredientRequestValidator()
    {
        RuleFor(x => x.CustomerPrice)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.QuantityUsed)
            .GreaterThan(0).WithMessage("Quantity used must be greater than zero");

        RuleFor(x => x.GroupName)
            .NotEmpty().WithMessage("Group name is required")
            .MaximumLength(50);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
