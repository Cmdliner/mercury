using System.Security.Claims;
using Mercury.Api.Data;
using Mercury.Api.DTOs;
using Mercury.Api.Services;
using Mercury.Merchants.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Mercury.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register/merchant")]
    public async Task<ActionResult<AuthResponse>> RegisterMerchant([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterMerchantAsync(request.Email, request.Password, request.MerchantName, request.OwnerName);
        return result.Success ? Ok(new AuthResponse(result.token!)) : BadRequest(result.Errors);
    }

    [HttpPost("register/staff")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<AuthResponse>> RegisterStaff([FromBody] RegisterStaffRequest request)
    {
        var merchantId = Guid.Parse(User.FindFirstValue("merchant_id")!);
        
        var result = await authService.RegisterStaffAsync(
            merchantId: merchantId,
            storeId: request.StoreId,
            role: request.Role,
            name: request.Name,
            email: request.Email,
            password: request.Password
            );
        if (!result.Success) return BadRequest(result.Errors);

        return Ok(new AuthResponse(result.token!));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var (success, token, error) = await authService.LoginAsync(request.Email, request.Password);
        return success ? Ok(new AuthResponse(token!)) : Unauthorized(error);
    }
}