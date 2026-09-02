using System.ComponentModel.DataAnnotations;

namespace Mercury.Api.DTOs;

public record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(6)] string Password,
    [property: Required, MaxLength(128)] string MerchantName,
    [property: Required, MaxLength(128)] string OwnerName);

public record LoginRequest(
    [property: Required, EmailAddress] string Email, 
    [property: Required, MinLength(6)] string Password);

public record AuthResponse(string Token);