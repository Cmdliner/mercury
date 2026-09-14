namespace Mercury.Payments;

public class PaystackOptions
{
    public required string SecretKey { get;  init; }
    public required string BaseUrl { get; init; } = "https://api.paystack.co";
}