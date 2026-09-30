namespace Mercury.Payments;

public class PaymentProviderException(string provider, int statusCode, string providerMessage)
    : Exception($"{provider} rejected the request ({statusCode}): {providerMessage}")
{
    public string Provider { get; } = provider;
    public int StatusCode { get; } = statusCode;
    public string ProviderMessage { get; } = providerMessage;
}
