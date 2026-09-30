using System.ComponentModel.DataAnnotations;
using Mercury.Merchants.Entities;

namespace Mercury.Api.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required, MaxLength(128)] string MerchantName,
    [Required, MaxLength(128)] string OwnerName);

public record RegisterStaffRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    [Required] Guid StoreId,
    [Required, MaxLength(128)] string Name,
    [Required] StaffRole Role);

public record LoginRequest(
    [Required, EmailAddress] string Email, 
    [Required, MinLength(6)] string Password);

public record AuthResponse(string Token);