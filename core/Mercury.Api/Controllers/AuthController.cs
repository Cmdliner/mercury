using Mercury.Api.Data;
using Mercury.Api.DTOs;
using Mercury.Api.Services;
using Mercury.Merchants.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Mercury.Api.Controllers;

[ApiController]
[Route("[controller]/auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request.Email, request.Password, request.MerchantName, request.OwnerName);
        return result.Success ? Ok(new AuthResponse(result.token)) : BadRequest(result.Errors);
    }

    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var (success, token, error) = await authService.LoginAsync(request.Email, request.Password);
        return success ? Ok(new AuthResponse(token!)) : Unauthorized(error);
    }
}