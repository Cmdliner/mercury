using Mercury.Ledger.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercury.Ledger.Services;

public class AccountProvisioningService(DbContext db)
{
    public async Task<(Guid PendingSettlementId, Guid RevenueId, Guid RefundsId)> ProvisionStoreAccountsAsync(
        Guid storeId,
        Guid storeName)
    {
        var pendingSettlementAccount = new Account
        {
            Name = $"{storeName} — Pending Settlement", 
            Code = $"STORE-{storeId}-PENDING", 
            Type = AccountType.Asset,
            StoreId = storeId
        };
        var revenueAccount = new Account
        {
            Name = $"{storeName} — Sales Revenue", 
            Code = $"STORE-{storeId}-REVENUE", 
            Type = AccountType.Asset,
            StoreId = storeId
        };
        var refundsAccount = new Account
        {
            Name = $"{storeName} — Refunds", 
            Code = $"STORE-{storeId}-REFUNDS", 
            Type = AccountType.Expense,
            StoreId = storeId
        };
        
        db.Set<Account>().AddRange(pendingSettlementAccount, revenueAccount, refundsAccount);
        await db.SaveChangesAsync();
        
        return (pendingSettlementAccount.Id, revenueAccount.Id, refundsAccount.Id);
    }
}