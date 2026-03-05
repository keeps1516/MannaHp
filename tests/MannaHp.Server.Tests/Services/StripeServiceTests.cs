using FluentAssertions;
using MannaHp.Server.Services;
using Microsoft.Extensions.Configuration;

namespace MannaHp.Server.Tests.Services;

public class StripeServiceTests
{
    [Fact]
    public void PublishableKey_ReturnsConfiguredValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = "sk_test_fake",
                ["Stripe:PublishableKey"] = "pk_test_expected_key",
            })
            .Build();

        var service = new StripeService(config);
        service.PublishableKey.Should().Be("pk_test_expected_key");
    }

    [Fact]
    public void Constructor_MissingSecretKey_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:PublishableKey"] = "pk_test_fake",
            })
            .Build();

        var act = () => new StripeService(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SecretKey*");
    }

    [Fact]
    public void Constructor_MissingPublishableKey_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = "sk_test_fake",
            })
            .Build();

        var act = () => new StripeService(config);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*PublishableKey*");
    }

    [Fact]
    public void Constructor_ValidConfig_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Stripe:SecretKey"] = "sk_test_valid",
                ["Stripe:PublishableKey"] = "pk_test_valid",
            })
            .Build();

        var act = () => new StripeService(config);
        act.Should().NotThrow();
    }
}
