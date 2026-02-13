using FluentValidation.TestHelper;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Validators;

namespace MannaHp.Shared.Tests.Validators;

public class CreateRecipeIngredientRequestValidatorTests
{
    private readonly CreateRecipeIngredientRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        _validator.TestValidate(new CreateRecipeIngredientRequest(Guid.NewGuid(), 2.0m)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyIngredientId_ShouldFail()
    {
        _validator.TestValidate(new CreateRecipeIngredientRequest(Guid.Empty, 2.0m)).ShouldHaveValidationErrorFor(x => x.IngredientId);
    }

    [Fact]
    public void Validate_ZeroQuantity_ShouldFail()
    {
        _validator.TestValidate(new CreateRecipeIngredientRequest(Guid.NewGuid(), 0m)).ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_NegativeQuantity_ShouldFail()
    {
        _validator.TestValidate(new CreateRecipeIngredientRequest(Guid.NewGuid(), -1m)).ShouldHaveValidationErrorFor(x => x.Quantity);
    }
}

public class UpdateRecipeIngredientRequestValidatorTests
{
    private readonly UpdateRecipeIngredientRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        _validator.TestValidate(new UpdateRecipeIngredientRequest(5.0m)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ZeroQuantity_ShouldFail()
    {
        _validator.TestValidate(new UpdateRecipeIngredientRequest(0m)).ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_NegativeQuantity_ShouldFail()
    {
        _validator.TestValidate(new UpdateRecipeIngredientRequest(-1m)).ShouldHaveValidationErrorFor(x => x.Quantity);
    }
}
