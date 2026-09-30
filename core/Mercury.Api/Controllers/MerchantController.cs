
using System.Security.Claims;
using Mercury.Api.Data;
using Mercury.Api.DTOs;
using Mercury.Merchants.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mercury.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class MerchantController(AppDbContext db) : ControllerBase
{
    
    [HttpPost("stores")]
    // [Authorize(Roles = "Owner")]
    public async Task<ActionResult<StoreCreateResponse>> CreateStore([FromBody] StoreCreateRequest request)
    {
        var merchantId = Guid.Parse(User.FindFirstValue("merchant_id")!);
        
        var merchant = db.Merchants.FirstOrDefault(m => m.Id == merchantId);
        if (merchant is null) return BadRequest("Merchant not found");
        var store = new Store
        {
            Name =  request.Name,
            Location = request.Location,
        };
        
        store.SetMerchant(merchantId);
        
        db.Stores.Add(store);
        await db.SaveChangesAsync();
        
        return Ok(new StoreCreateResponse(store.Name, store.Id));
    }

    [HttpGet("stores")]
    public async Task<ActionResult<StoresListResponse>> GetStores()
    {
        var merchantId = Guid.Parse(User.FindFirstValue("merchant_id")!);
        
        var merchant = db.Merchants
            .Include(merchant => merchant.Stores)
            .FirstOrDefault(m => m.Id == merchantId);
        if (merchant is null) return NotFound("Merchant not found");
    
        var stores = merchant
            .Stores
            .Select( s  => new StoreResponse(s.Id, s.Name, s.Location));
        
        return Ok(new StoresListResponse(merchant.Name, stores));
    }
}