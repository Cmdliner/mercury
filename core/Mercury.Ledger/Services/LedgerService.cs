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


    public async Task<JournalEntry> PostReversalAsync(
        Guid revenueAccountId,
        Guid pendingSettlementAccountId,
        decimal amount,
        string reference,
        PaymentChannel channel)
    {
        await Task.Delay(0);
        var entry = JournalEntry.Create(
            reference: reference,
            channel: channel,
            lines:
            [
                new JournalLine { AccountId = revenueAccountId, Direction = EntryDirection.Credit, Amount = amount },
                new JournalLine
                    { AccountId = pendingSettlementAccountId, Direction = EntryDirection.Debit, Amount = amount }
            ]);

        db.Set<JournalEntry>().Add(entry);
        await db.SaveChangesAsync();

        return entry;
    }


    public async Task<JournalEntry> PostRefundAsync(
        Guid refundAccountId,
        Guid paymentSettlementAccountId,
        decimal amount,
        string reference,
        PaymentChannel channel)
    {
        var entry = JournalEntry.Create(
            reference,
            channel,
            lines:
            [
                new JournalLine { AccountId = refundAccountId, Amount = amount, Direction = EntryDirection.Debit },
                new JournalLine { AccountId = paymentSettlementAccountId, Amount = amount, Direction = EntryDirection.Credit }
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

    public async Task<Dictionary<Guid, decimal>> GetAccountBalancesAsync(IEnumerable<Guid> accountIds)
    {
        var ids = accountIds.ToList();
        var accounts = await db.Set<Account>()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id);

        var sums = await db.Set<JournalLine>()
            .Where(l => ids.Contains(l.AccountId))
            .GroupBy(l => l.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Debits = g.Where(l => l.Direction == EntryDirection.Debit).Sum(l => l.Amount),
                Credits = g.Where(l => l.Direction == EntryDirection.Credit).Sum(l => l.Amount)
            })
            .ToDictionaryAsync(x => x.AccountId);

        Dictionary<Guid, decimal> accountBalances = [];
        foreach (var id in ids)
        {
            if (!accounts.TryGetValue(id, out var account))
                throw new ArgumentException($"Account with id: {id} not found");

            var (debits, credits) = sums.TryGetValue(id, out var s) ? (s.Debits, s.Credits) : (0M, 0M);
            var balance = account.Type.NormalBalanceSide() switch
            {
                EntryDirection.Debit => debits - credits,
                EntryDirection.Credit => credits - debits,
                _ => throw new InvalidOperationException("Unhandled normal balance side")
            };
            accountBalances.TryAdd(id,
                balance); // Tries to add it if the id doesn't already exist. if the key exists it skips instead of throwing an exception
        }


        return accountBalances;
    }
}