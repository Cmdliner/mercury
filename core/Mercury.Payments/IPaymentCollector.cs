using System.Collections;
using Mercury.Payments.Entities;

namespace Mercury.Payments;

public interface IPaymentCollector
{
    PaymentProvider Provider { get; }
    Task<PaymentInitiationResult> InitiateAsync(decimal amount, string reference, CancellationToken ct);
    bool VerifyWebhookSignature(string rawPayload, IReadOnlyDictionary<string, string> headers);
    PaymentWebhookEvent ParseWebhookPayload(string rawPayload);
}


public record PaymentInitiationResult(string ProviderReference, string PaymentInstructions);
public record PaymentWebhookEvent(string ProviderReference, decimal Amount, bool Sucessful);