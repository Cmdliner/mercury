using Mercury.Api.Data;
using Mercury.Merchants.Entities;
using Microsoft.AspNetCore.Identity;

namespace Mercury.Api.Services;

public class AuthService(UserManager<IdentityUser<Guid>> userManager, AppDbContext db, TokenService tokenService)
{
    public async Task<(bool Success, string? token, IEnumerable<string> Errors)> RegisterAsync(
        string email,
        string password,
        string merchantName,
        string ownerName
    )
    {
        var identityUser = new IdentityUser<Guid> { UserName = email, Email = email };
        var createResult = await userManager.CreateAsync(identityUser, password);

        if (!createResult.Succeeded) return (false, null, createResult.Errors.Select(e => e.Description));

        var merchant = new Merchant { Name = merchantName };
        db.Merchants.Add(merchant);

        var owner = Staff.Create(
            name: ownerName,
            role: StaffRole.Owner,
            merchantId: merchant.Id,
            identityUserId: identityUser.Id,
            storeId: null);

        db.StaffMembers.Add(owner);
        await db.SaveChangesAsync();

        var token = tokenService.GenerateToken(identityUser, owner);
        return (true, token, []);
    }

    public async Task<(bool Success, string? token, string? Error)> LoginAsync(string email, string password)
    {
        var identityUser = await userManager.FindByEmailAsync(email);
        if (identityUser is null) return (false, null, "Invalid credentials");

        var passwordValid = await userManager.CheckPasswordAsync(identityUser, password);
        if (!passwordValid) return (false, null, "Invalid credentials");

        var staff = await db.StaffMembers.FindAsync(identityUser.Id);
        if (staff is null) return (false, null, "Invalid credentials");
        
        var token= tokenService.GenerateToken(identityUser, staff);
        return (true, token, null);
    }
}