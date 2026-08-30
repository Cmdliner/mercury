using Mercury.Ledger.Entities;
using Mercury.Ledger.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Mercury.Ledger.Services;

public class LedgerService(DbContext db)
{
    public async Task<JournalEntry> PostSaleAsync(
        Guid pendingSettlementAccountId,
        Guid revenueAccountId,
        decimal amount,
        string reference,
        PaymentChannel channel)
    {
        await Task.Delay(0);
        var lines = new List<JournalLine> { };
        var entry = JournalEntry.Create(
            reference: reference,
            channel: channel,
            lines:
            [
                new JournalLine
                    { AccountId = pendingSettlementAccountId, Direction = EntryDirection.Debit, Amount = amount },
                new JournalLine { AccountId = revenueAccountId, Direction = EntryDirection.Credit, Amount = amount }
            ]);

        db.Set<JournalEntry>().Add(entry);
        await db.SaveChangesAsync();

        return entry;
    }

    public async Task<decimal> GetAccountBalanceAsync(Guid accountId)
    {
        var account = await db.Set<Account>().FindAsync(accountId) ??
                      throw new InvalidOperationException($"Account {accountId} not found");

        var debits = await db.Set<JournalLine>()
            .Where(l => l.AccountId == accountId && l.Direction == EntryDirection.Debit)
            .SumAsync(l => l.Amount);

        var credits = await db.Set<JournalLine>()
            .Where(l => l.AccountId == accountId && l.Direction == EntryDirection.Credit)
            .SumAsync(l => l.Amount);

        return account.Type.NormalBalanceSide() switch
        {
            EntryDirection.Debit => debits - credits,
            EntryDirection.Credit => credits - debits,
            _ => throw new InvalidOperationException("Unhandled normal balance side")
        };
    }
}