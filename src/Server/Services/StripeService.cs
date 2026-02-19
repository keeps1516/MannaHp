using Stripe;

namespace MannaHp.Server.Services;

public class StripeService
{
    private readonly string _publishableKey;

    public StripeService(IConfiguration config)
    {
        var secretKey = config["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey is not configured");
        _publishableKey = config["Stripe:PublishableKey"]
            ?? throw new InvalidOperationException("Stripe:PublishableKey is not configured");

        StripeConfiguration.ApiKey = secretKey;
    }

    public string PublishableKey => _publishableKey;

    public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string? description = null)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100), // Stripe uses cents
            Currency = "usd",
            Description = description,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
        };

        var service = new PaymentIntentService();
        return await service.CreateAsync(options);
    }

    public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
    {
        var service = new PaymentIntentService();
        return await service.GetAsync(paymentIntentId);
    }

    public async Task<Charge> GetChargeAsync(string chargeId)
    {
        var service = new ChargeService();
        return await service.GetAsync(chargeId);
    }
}
