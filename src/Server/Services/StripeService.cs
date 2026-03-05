using Stripe;

namespace MannaHp.Server.Services;

public class StripeService
{
    private readonly string? _publishableKey;

    public bool IsConfigured { get; }

    public StripeService(IConfiguration config)
    {
        var secretKey = config["Stripe:SecretKey"];
        _publishableKey = config["Stripe:PublishableKey"];

        IsConfigured = !string.IsNullOrEmpty(secretKey) && !string.IsNullOrEmpty(_publishableKey);

        if (IsConfigured)
        {
            StripeConfiguration.ApiKey = secretKey;
        }
    }

    public string PublishableKey => _publishableKey
        ?? throw new InvalidOperationException("Stripe is not configured");

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
