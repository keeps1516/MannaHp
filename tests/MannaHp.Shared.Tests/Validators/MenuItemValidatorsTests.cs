using FluentValidation.TestHelper;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Validators;

namespace MannaHp.Shared.Tests.Validators;

public class CreateMenuItemRequestValidatorTests
{
    private readonly CreateMenuItemRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var result = _validator.TestValidate(new CreateMenuItemRequest(Guid.NewGuid(), "Latte", "Espresso + milk", false, 1));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCategoryId_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateMenuItemRequest(Guid.Empty, "Latte", null, false, 0));
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_EmptyName_ShouldFail(string? name)
    {
        var result = _validator.TestValidate(new CreateMenuItemRequest(Guid.NewGuid(), name!, null, false, 0));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceedsMaxLength_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateMenuItemRequest(Guid.NewGuid(), new string('x', 101), null, false, 0));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativeSortOrder_ShouldFail()
    {
        var result = _validator.TestValidate(new CreateMenuItemRequest(Guid.NewGuid(), "Latte", null, false, -1));
        result.ShouldHaveValidationErrorFor(x => x.SortOrder);
    }
}

public class UpdateMenuItemRequestValidatorTests
{
    private readonly UpdateMenuItemRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var result = _validator.TestValidate(new UpdateMenuItemRequest("Latte", "Espresso + milk", false, Guid.NewGuid(), 1, true));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyCategoryId_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateMenuItemRequest("Latte", null, false, Guid.Empty, 0, true));
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        var result = _validator.TestValidate(new UpdateMenuItemRequest("", null, false, Guid.NewGuid(), 0, true));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
