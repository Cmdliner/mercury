using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Mercury.Merchants.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Mercury.Api.Services;

public class TokenService(IConfiguration config)
{
    public string GenerateToken(IdentityUser<Guid> user, Staff staff)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("role", staff.Role.ToString()),
            new("merchant_id", staff.MerchantId.ToString()),
        };

        if (staff.StoreId is not null) claims.Add(new("store_id", staff.StoreId.ToString()!));
        
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:SigningKey"]!));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);

    }
}