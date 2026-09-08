namespace Mercury.Payments.Entities;

public enum PaymentProvider { Nomba, Paystack }
public enum PaymentStatus { Pending, Successful, Failed }

public class PaymentRequest
{
    public Guid Id { get; init; }
    public Guid MerchantId { get; init; }
    public Guid StoreId { get; init; }
    public decimal Amount { get; init; }
    public PaymentProvider Provider { get; init; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? ProviderReference { get; set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    
    public void MarkSuccessful()
    {
        Status = PaymentStatus.Successful;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = PaymentStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}