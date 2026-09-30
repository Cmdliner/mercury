using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Mercury.Api.Data;
using Mercury.Payments;
using Mercury.Payments.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercury.Api.Controllers;

public record InitiatePaymentRequest(
    [Required, Range(0.01, double.MaxValue)] decimal Amount, 
    [Required] PaymentProvider Provider,
    [Required] Guid IdempotencyKey);

public record InitiatePaymentResponse();

[ApiController]
[Route("api/v1/payments")]
[Authorize]
public class PaymentController(
    PaymentCollectorFactory paymentCollectorFactory,
    AppDbContext db) : ControllerBase
{
    
    [HttpPost("initialize")]
    public async Task<ActionResult<InitiatePaymentResponse>> InitiatePayment([FromBody] InitiatePaymentRequest request, CancellationToken ct)
    {
        var merchantId = Guid.Parse(User.FindFirstValue("merchant_id")!);
        var storeIdClaim = User.FindFirstValue("store_id");
        if (storeIdClaim is null) return BadRequest("Only staff assigned to store can initiate a sale");
        
        var reference = Guid.NewGuid().ToString();

        var existing = db.PaymentRequests.FirstOrDefault(pr => pr.IdempotencyKey == request.IdempotencyKey);
        if(existing is not null) return Ok(new PaymentInitiationResult(existing.ProviderReference!, string.Empty));
        

        var paymentRequest = new PaymentRequest
        {
            MerchantId = merchantId,
            StoreId = Guid.Parse(storeIdClaim),
            Amount = request.Amount,
            Provider = request.Provider,
            IdempotencyKey = request.IdempotencyKey,
        };
        
        db.PaymentRequests.Add(paymentRequest);
        await db.SaveChangesAsync(ct);
        
        var collector = paymentCollectorFactory.GetCollector(request.Provider);
        var result = await collector.InitiateAsync(request.Amount, paymentRequest.Id.ToString(), ct);
        
        return Ok(result);
    }
    
}