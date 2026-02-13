using FluentValidation;
using MannaHp.Shared.DTOs;

namespace MannaHp.Shared.Validators;

public class CreateRecipeIngredientRequestValidator : AbstractValidator<CreateRecipeIngredientRequest>
{
    public CreateRecipeIngredientRequestValidator()
    {
        RuleFor(x => x.IngredientId)
            .NotEmpty().WithMessage("Ingredient is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");
    }
}

public class UpdateRecipeIngredientRequestValidator : AbstractValidator<UpdateRecipeIngredientRequest>
{
    public UpdateRecipeIngredientRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero");
    }
}
