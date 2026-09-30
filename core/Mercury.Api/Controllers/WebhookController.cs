using Mercury.Api.Data;
using Mercury.Ledger.Entities;
using Mercury.Ledger.Services;
using Mercury.Payments;
using Mercury.Payments.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Mercury.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]s")]
public class WebhookController(AppDbContext db): ControllerBase
{
    [HttpPost("paystack")]
    [AllowAnonymous]    
    public async Task<IActionResult> HandlePaystackWebhook(
        [FromServices] IPaymentCollector collector,
        [FromServices] LedgerService ledgerService,
        [FromServices] IConnectionMultiplexer redis)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync();
        var headers = Request
            .Headers
            .ToDictionary(h => h.Key.ToLowerInvariant(), h => h.Value.ToString());

        if (!collector.VerifyWebhookSignature(rawBody, headers)) return Unauthorized();
        
        var evt = collector.ParseWebhookPayload(rawBody);
        if (!evt.Successful) return Ok();

        var redisDb = redis.GetDatabase();
        var idempotencyKey = $"webhook:{evt.ProviderReference}";

        if (!await redisDb.StringSetAsync(idempotencyKey, "processing", TimeSpan.FromHours(24), When.NotExists))
        {
            return Ok();
        }

        var paymentRequest = await db.Set<PaymentRequest>()
            .FirstOrDefaultAsync(p => p.Id == Guid.Parse(evt.ProviderReference));
        if (paymentRequest is null) return Ok();

        var accounts = await db.Set<Account>()
            .Where(a => a.StoreId == paymentRequest.StoreId && (a.Code.EndsWith("-PENDING") || a.Code.EndsWith("-REVENUE")))
            .ToListAsync();

        var pendingId = accounts.First(a => a.Name.EndsWith("-PENDING")).Id;
        var revenueId = accounts.First(a => a.Name.EndsWith("-REVENUE")).Id;
        
        await ledgerService.PostSaleAsync(pendingId, revenueId, paymentRequest.Amount, evt.ProviderReference, PaymentChannel.BankTransfer);
        paymentRequest.MarkSuccessful();

        await db.SaveChangesAsync();
        return Ok();
    }
}