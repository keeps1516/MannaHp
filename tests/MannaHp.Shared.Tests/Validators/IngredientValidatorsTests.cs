using FluentAssertions;
using FluentValidation.TestHelper;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;
using MannaHp.Shared.Validators;

namespace MannaHp.Shared.Tests.Validators;

public class CreateIngredientRequestValidatorTests
{
    private readonly CreateIngredientRequestValidator _validator = new();

    private static CreateIngredientRequest Valid() =>
        new("Chicken", UnitOfMeasure.Oz, 0.3125m, 400m, 64m);

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_EmptyName_ShouldFail(string? name)
    {
        var result = _validator.TestValidate(new CreateIngredientRequest(name!, UnitOfMeasure.Oz, 0.10m, 100m, 10m));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateIngredientRequest(new string('a', 101), UnitOfMeasure.Oz, 0.10m, 100m, 10m));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_InvalidUnit_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateIngredientRequest("Test", (UnitOfMeasure)999, 0.10m, 100m, 10m));
        result.ShouldHaveValidationErrorFor(x => x.Unit);
    }

    [Fact]
    public void Validate_NegativeCost_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateIngredientRequest("Test", UnitOfMeasure.Oz, -1m, 100m, 10m));
        result.ShouldHaveValidationErrorFor(x => x.CostPerUnit);
    }

    [Fact]
    public void Validate_NegativeStock_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateIngredientRequest("Test", UnitOfMeasure.Oz, 0.10m, -1m, 10m));
        result.ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }

    [Fact]
    public void Validate_NegativeThreshold_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateIngredientRequest("Test", UnitOfMeasure.Oz, 0.10m, 100m, -1m));
        result.ShouldHaveValidationErrorFor(x => x.LowStockThreshold);
    }
}

public class UpdateIngredientRequestValidatorTests
{
    private readonly UpdateIngredientRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var result = _validator.TestValidate(new UpdateIngredientRequest("Chicken", UnitOfMeasure.Oz, 0.31m, 400m, 64m, true));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateIngredientRequest("", UnitOfMeasure.Oz, 0.10m, 100m, 10m, true));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativeCost_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateIngredientRequest("Test", UnitOfMeasure.Oz, -1m, 100m, 10m, true));
        result.ShouldHaveValidationErrorFor(x => x.CostPerUnit);
    }
}
