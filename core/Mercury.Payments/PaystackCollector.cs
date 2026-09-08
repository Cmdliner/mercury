using System.Collections;
using Mercury.Payments.Entities;

namespace Mercury.Payments;

public class PaystackCollector: IPaymentCollector
{
    public PaymentProvider Provider { get; } = PaymentProvider.Paystack;
    
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