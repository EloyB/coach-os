using System.Collections.Concurrent;
using CoachOS.Application.Abstractions;
using CoachOS.Application.Configuration;
using CoachOS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoachOS.Infrastructure.Mollie;

/// <summary>
/// Dev-fallback voor <see cref="IBillingMollieClient"/> zolang er geen platform-API-key is.
/// Houdt betalingen in het geheugen bij (singleton). De checkout-URL wijst naar de eigen
/// abonnementspagina met <c>?fakePayment=</c>; daar kan je in Development de betaling laten
/// slagen of mislukken via het dev-endpoint (<see cref="SetFakeStatus"/>).
/// </summary>
public class FakeBillingMollieClient(
    IOptions<AppOptions> appOptions,
    TimeProvider timeProvider,
    ILogger<FakeBillingMollieClient> logger) : IBillingMollieClient
{
    private readonly ConcurrentDictionary<string, BillingPaymentDetails> _payments = new();
    private readonly string _frontendBaseUrl = appOptions.Value.FrontendBaseUrl.TrimEnd('/');

    public bool IsConfigured => false;

    public Task<Result<string>> CreateCustomerAsync(string name, string email, CancellationToken ct = default)
    {
        string id = $"cst_fake_{Guid.NewGuid():N}";
        logger.LogInformation("FakeBillingMollieClient: customer {Id} aangemaakt", id);
        return Task.FromResult(Result<string>.Ok(id));
    }

    public Task<Result<BillingPaymentResult>> CreatePaymentAsync(BillingPaymentRequest request, CancellationToken ct = default)
        => Task.FromResult(Create(request, "oneoff"));

    public Task<Result<BillingPaymentResult>> CreateFirstPaymentAsync(BillingPaymentRequest request, CancellationToken ct = default)
        => Task.FromResult(string.IsNullOrEmpty(request.CustomerId)
            ? Result<BillingPaymentResult>.Fail(new Error(ErrorCodes.Validation, "Een eerste betaling vereist een Mollie customer."))
            : Create(request, "first"));

    public Task<Result<BillingPaymentDetails>> GetPaymentDetailsAsync(string paymentId, CancellationToken ct = default)
        => Task.FromResult(_payments.TryGetValue(paymentId, out BillingPaymentDetails? details)
            ? Result<BillingPaymentDetails>.Ok(details)
            : Result<BillingPaymentDetails>.Fail(new Error(ErrorCodes.ExternalService, "Onbekende (fake) Mollie-betaling.")));

    public Task<Result<string>> CreateSubscriptionAsync(BillingSubscriptionRequest request, CancellationToken ct = default)
    {
        string id = $"sub_fake_{Guid.NewGuid():N}";
        logger.LogInformation("FakeBillingMollieClient: subscription {Id} ({Interval}, start {Start})",
            id, request.Interval, request.StartDate);
        return Task.FromResult(Result<string>.Ok(id));
    }

    public Task<Result> CancelSubscriptionAsync(string customerId, string subscriptionId, CancellationToken ct = default)
    {
        logger.LogInformation("FakeBillingMollieClient: subscription {Id} geannuleerd", subscriptionId);
        return Task.FromResult(Result.Ok());
    }

    public Task<Result> RefundPaymentAsync(string paymentId, decimal amount, string description, CancellationToken ct = default)
    {
        logger.LogInformation("FakeBillingMollieClient: terugbetaling van {Amount} op {Id}", amount, paymentId);
        return Task.FromResult(Result.Ok());
    }

    /// <summary>Dev/test: zet de status van een fake-betaling (open, paid, failed, canceled, expired).</summary>
    public bool SetFakeStatus(string paymentId, string status)
    {
        if (!_payments.TryGetValue(paymentId, out BillingPaymentDetails? current))
            return false;

        DateTime? paidAt = status == "paid" ? timeProvider.GetUtcNow().UtcDateTime : null;
        _payments[paymentId] = current with { Status = status, PaidAt = paidAt };
        return true;
    }

    /// <summary>Dev/test: simuleert een automatische verlenging die Mollie aanmaakt voor een subscription.</summary>
    public string AddFakeRecurringPayment(string subscriptionId, string customerId, decimal amount, string status)
    {
        string id = $"tr_fake_{Guid.NewGuid():N}";
        DateTime? paidAt = status == "paid" ? timeProvider.GetUtcNow().UtcDateTime : null;
        _payments[id] = new BillingPaymentDetails(
            id, status, amount, paidAt, "recurring", customerId, subscriptionId, new Dictionary<string, string>());
        return id;
    }

    private Result<BillingPaymentResult> Create(BillingPaymentRequest request, string sequenceType)
    {
        string id = $"tr_fake_{Guid.NewGuid():N}";
        _payments[id] = new BillingPaymentDetails(
            id, "open", request.Amount, null, sequenceType, request.CustomerId, null,
            request.Metadata ?? new Dictionary<string, string>());
        string checkoutUrl = $"{_frontendBaseUrl}/dashboard/settings/subscription?fakePayment={id}";
        logger.LogInformation("FakeBillingMollieClient: {Sequence}-betaling {Id} van {Amount} EUR", sequenceType, id, request.Amount);
        return Result<BillingPaymentResult>.Ok(new BillingPaymentResult(id, "open", checkoutUrl));
    }
}
