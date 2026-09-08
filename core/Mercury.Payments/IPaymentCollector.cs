using System.Collections;
using Mercury.Payments.Entities;

namespace Mercury.Payments;

public interface IPaymentCollector
{
    PaymentProvider Provider { get; }
    Task<PaymentInitiationResult> InitiateAsync();
    bool VerifyRawWebhookSignature(string rawPayload, IDictionary headers);
    PaymentWebhookEvent ParseWehookPayload(string rawPayload);
}


public record PaymentInitiationResult(string ProviderReference, string PaymentInstructions);
public record PaymentWebhookEvent(string ProviderReference, decimal Amount, bool Sucessful);