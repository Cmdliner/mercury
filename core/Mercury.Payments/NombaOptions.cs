namespace Mercury.Payments;

public class NombaOptions
{
    public required string BaseUrl { get; init; } = "https://sandbox.nomba.com";
    public required string ClientId { get; set; }
    public required string AccountId { get; set; }
    public required string PrivateKey { get; set; }
    
}