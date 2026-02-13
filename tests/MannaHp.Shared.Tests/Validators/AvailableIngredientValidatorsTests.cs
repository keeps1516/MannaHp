using FluentValidation.TestHelper;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Validators;

namespace MannaHp.Shared.Tests.Validators;

public class CreateAvailableIngredientRequestValidatorTests
{
    private readonly CreateAvailableIngredientRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var req = new CreateAvailableIngredientRequest(Guid.NewGuid(), 3.00m, 8.0m, false, "Proteins", 1);
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyIngredientId_ShouldFail()
    {
        var req = new CreateAvailableIngredientRequest(Guid.Empty, 3.00m, 8.0m, false, "Proteins", 1);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.IngredientId);
    }

    [Fact]
    public void Validate_NegativeCustomerPrice_ShouldFail()
    {
        var req = new CreateAvailableIngredientRequest(Guid.NewGuid(), -1m, 8.0m, false, "Proteins", 1);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.CustomerPrice);
    }

    [Fact]
    public void Validate_ZeroQuantityUsed_ShouldFail()
    {
        var req = new CreateAvailableIngredientRequest(Guid.NewGuid(), 3.00m, 0m, false, "Proteins", 1);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.QuantityUsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_EmptyGroupName_ShouldFail(string? group)
    {
        var req = new CreateAvailableIngredientRequest(Guid.NewGuid(), 3.00m, 8.0m, false, group!, 1);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.GroupName);
    }

    [Fact]
    public void Validate_GroupNameExceedsMaxLength_ShouldFail()
    {
        var req = new CreateAvailableIngredientRequest(Guid.NewGuid(), 3.00m, 8.0m, false, new string('x', 51), 1);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.GroupName);
    }

    [Fact]
    public void Validate_NegativeSortOrder_ShouldFail()
    {
        var req = new CreateAvailableIngredientRequest(Guid.NewGuid(), 3.00m, 8.0m, false, "Proteins", -1);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.SortOrder);
    }
}

public class UpdateAvailableIngredientRequestValidatorTests
{
    private readonly UpdateAvailableIngredientRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var req = new UpdateAvailableIngredientRequest(3.00m, 8.0m, false, "Proteins", 1, true);
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ZeroQuantityUsed_ShouldFail()
    {
        var req = new UpdateAvailableIngredientRequest(3.00m, 0m, false, "Proteins", 1, true);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.QuantityUsed);
    }

    [Fact]
    public void Validate_EmptyGroupName_ShouldFail()
    {
        var req = new UpdateAvailableIngredientRequest(3.00m, 8.0m, false, "", 1, true);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.GroupName);
    }
}
