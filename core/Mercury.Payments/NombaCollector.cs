using System.Collections;
using Mercury.Payments.Entities;

namespace Mercury.Payments;

public class NombaCollector(IHttpClientFactory httpClientFactory, INombaTokenProvider tokenProvider): IPaymentCollector
{
    public PaymentProvider Provider { get; } = PaymentProvider.Nomba;
    

    public async Task<PaymentInitiationResult> InitiateAsync(decimal amount, string reference, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("Nomba");
        var token = await tokenProvider.GetAccessTokenAsync(ct);
        
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        // await client.PostAsync();
        throw new NotImplementedException();
    }

    public bool VerifyWebhookSignature(string rawPayload, IReadOnlyDictionary<string, string> headers)
    {
        throw new NotImplementedException();
    }

    public PaymentWebhookEvent ParseWebhookPayload(string rawPayload)
    {
        throw new NotImplementedException();
    }
}