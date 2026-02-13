using FluentValidation.TestHelper;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Validators;

namespace MannaHp.Shared.Tests.Validators;

public class CreateVariantRequestValidatorTests
{
    private readonly CreateVariantRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        _validator.TestValidate(new CreateVariantRequest("12oz", 4.75m, 1)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_EmptyName_ShouldFail(string? name)
    {
        _validator.TestValidate(new CreateVariantRequest(name!, 4.75m, 0)).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldFail()
    {
        _validator.TestValidate(new CreateVariantRequest(new string('x', 51), 4.75m, 0)).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativePrice_ShouldFail()
    {
        _validator.TestValidate(new CreateVariantRequest("12oz", -1m, 0)).ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_ZeroPrice_ShouldPass()
    {
        _validator.TestValidate(new CreateVariantRequest("Regular", 0m, 0)).ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_NegativeSortOrder_ShouldFail()
    {
        _validator.TestValidate(new CreateVariantRequest("12oz", 4.75m, -1)).ShouldHaveValidationErrorFor(x => x.SortOrder);
    }
}

public class UpdateVariantRequestValidatorTests
{
    private readonly UpdateVariantRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        _validator.TestValidate(new UpdateVariantRequest("16oz", 5.25m, 2, true)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        _validator.TestValidate(new UpdateVariantRequest("", 5.25m, 0, true)).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativePrice_ShouldFail()
    {
        _validator.TestValidate(new UpdateVariantRequest("16oz", -1m, 0, true)).ShouldHaveValidationErrorFor(x => x.Price);
    }
}
