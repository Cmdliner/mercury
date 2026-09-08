using System.Collections;
using Mercury.Payments.Entities;

namespace Mercury.Payments;

public class NombaCollector: IPaymentCollector
{
    public PaymentProvider Provider { get; } = PaymentProvider.Nomba;
    
    public async Task<PaymentInitiationResult> InitiateAsync()
    {
        await Task.Delay(0);
        throw new NotImplementedException();
    }

    public bool VerifyRawWebhookSignature(string rawPayload, IDictionary headers)
    {
        throw new NotImplementedException();
    }

    public PaymentWebhookEvent ParseWehookPayload(string rawPayload)
    {
        throw new NotImplementedException();
    }
}