using FluentAssertions;
using FluentValidation.TestHelper;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Validators;

namespace MannaHp.Shared.Tests.Validators;

public class CreateCategoryRequestValidatorTests
{
    private readonly CreateCategoryRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var result = _validator.TestValidate(new CreateCategoryRequest("Bowls", 1));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_EmptyName_ShouldFail(string? name)
    {
        var result = _validator.TestValidate(new CreateCategoryRequest(name!, 0));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateCategoryRequest(new string('a', 101), 0));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativeSortOrder_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateCategoryRequest("Valid", -1));
        result.ShouldHaveValidationErrorFor(x => x.SortOrder);
    }

    [Fact]
    public void Validate_ZeroSortOrder_ShouldPass()
    {
        var result = _validator.TestValidate(new CreateCategoryRequest("Valid", 0));
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }
}

public class UpdateCategoryRequestValidatorTests
{
    private readonly UpdateCategoryRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var result = _validator.TestValidate(new UpdateCategoryRequest("Bowls", 1, true));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateCategoryRequest("", 0, true));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativeSortOrder_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateCategoryRequest("Valid", -1, true));
        result.ShouldHaveValidationErrorFor(x => x.SortOrder);
    }
}
