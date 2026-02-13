using FluentValidation;
using MannaHp.Shared.DTOs;

namespace MannaHp.Shared.Validators;

public class CreateIngredientRequestValidator : AbstractValidator<CreateIngredientRequest>
{
    public CreateIngredientRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ingredient name is required")
            .MaximumLength(100);

        RuleFor(x => x.Unit)
            .IsInEnum().WithMessage("Invalid unit of measure");

        RuleFor(x => x.CostPerUnit)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0);
    }
}

public class UpdateIngredientRequestValidator : AbstractValidator<UpdateIngredientRequest>
{
    public UpdateIngredientRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ingredient name is required")
            .MaximumLength(100);

        RuleFor(x => x.Unit)
            .IsInEnum().WithMessage("Invalid unit of measure");

        RuleFor(x => x.CostPerUnit)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0);
    }
}
