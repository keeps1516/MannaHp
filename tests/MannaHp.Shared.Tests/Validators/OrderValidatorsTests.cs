using FluentValidation.TestHelper;
using MannaHp.Shared.DTOs;
using MannaHp.Shared.Enums;
using MannaHp.Shared.Validators;

namespace MannaHp.Shared.Tests.Validators;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    private static CreateOrderRequest ValidOrder() => new(
        PaymentMethod.InStore,
        null,
        [new CreateOrderItemRequest(Guid.NewGuid(), Guid.NewGuid(), 1, null, null)]);

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        _validator.TestValidate(ValidOrder()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_InvalidPaymentMethod_ShouldFail()
    {
        var req = new CreateOrderRequest((PaymentMethod)999, null,
            [new CreateOrderItemRequest(Guid.NewGuid(), Guid.NewGuid(), 1, null, null)]);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Fact]
    public void Validate_EmptyItems_ShouldFail()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null, []);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_NullItems_ShouldFail()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null, null!);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Validate_ItemWithEmptyMenuItemId_ShouldFail()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(Guid.Empty, Guid.NewGuid(), 1, null, null)]);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor("Items[0].MenuItemId");
    }

    [Fact]
    public void Validate_ItemWithZeroQuantity_ShouldFail()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(Guid.NewGuid(), Guid.NewGuid(), 0, null, null)]);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }

    [Fact]
    public void Validate_ItemWithNegativeQuantity_ShouldFail()
    {
        var req = new CreateOrderRequest(PaymentMethod.InStore, null,
            [new CreateOrderItemRequest(Guid.NewGuid(), Guid.NewGuid(), -1, null, null)]);
        var result = _validator.TestValidate(req);
        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
    }
}

public class UpdateOrderStatusRequestValidatorTests
{
    private readonly UpdateOrderStatusRequestValidator _validator = new();

    [Theory]
    [InlineData(OrderStatus.Received)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.Ready)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void Validate_ValidStatus_ShouldPass(OrderStatus status)
    {
        _validator.TestValidate(new UpdateOrderStatusRequest(status)).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_InvalidStatus_ShouldFail()
    {
        _validator.TestValidate(new UpdateOrderStatusRequest((OrderStatus)999)).ShouldHaveValidationErrorFor(x => x.Status);
    }
}
