using System.Collections;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mercury.Payments.Entities;
using Microsoft.Extensions.Options;

namespace Mercury.Payments;

public class PaystackCollector(HttpClient httpClient, IOptions<PaystackOptions> options) : IPaymentCollector
{
    public PaymentProvider Provider { get; } = PaymentProvider.Paystack;
    
    private readonly PaystackOptions _options = options.Value;
    
    public async Task<PaymentInitiationResult> InitiateAsync(decimal amount, string reference, CancellationToken ct)
    {
        var payload = new
        {
            email = $"sale-{reference}@mercury.internal",
            amount = (int)(amount * 100), // amount in kobo
            reference,
            bank_transfer = new { account_expires_at = DateTime.UtcNow.AddMinutes(30).ToString("o") }
        };

        var response = await httpClient.PostAsJsonAsync("charge", payload, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var data = body.GetProperty("data"); 
        var paystackReference = body.GetProperty("reference").GetString()!;
        
        // Use this as a placeholder for now till you get the actual response shape from paystack
        // You should probably log the data response to the console
        var instructions = data.ToString();
        
        return new PaymentInitiationResult(paystackReference, instructions);
    }

    public bool VerifyWebhookSignature(string rawPayload, IReadOnlyDictionary<string, string> headers)
    {
        var signature = headers["x-paystack-signature"].ToString();
        if(string.IsNullOrEmpty(signature))  return false;
        
        var secret = _options.SecretKey;
        var computedHash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(signature), Encoding.UTF8.GetBytes(rawPayload));
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();
        
        // Constant-time comparison:
        // A normal string comparison returns as soon as it finds the first mismatched character
        // meaning the time the comparison takes leaks information about how many characters matched before it failed.
        // In theory, an attacker measuring response times could exploit that to guess a valid signature byte-by-byte (a timing attack).
        // FixedTimeEquals always takes the same amount of time regardless of where the mismatch is, closing that leak.
        // This matters here specifically because the thing being compared is a security-critical signature
        // worth doing correctly rather than the "obviously correct-looking" == or .Equals()
        return CryptographicOperations
            .FixedTimeEquals(Encoding.UTF8.GetBytes(computedSignature), Encoding.UTF8.GetBytes(signature));
    }

    public PaymentWebhookEvent ParseWebhookPayload(string rawPayload)
    {
        var body =  JsonSerializer.Deserialize<JsonElement>(rawPayload);
        var eventType = body.GetProperty("event").GetString();
        var data = body.GetProperty("data");

        return new PaymentWebhookEvent(
            ProviderReference: data.GetProperty("reference").GetString()!,
            Amount: data.GetProperty("amount").GetInt32() / 100M,
            Sucessful: eventType == "charge.success");
    }
}