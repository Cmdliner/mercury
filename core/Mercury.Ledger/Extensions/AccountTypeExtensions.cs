using Mercury.Ledger.Entities;

namespace Mercury.Ledger.Extensions;

public static class AccountTypeExtensions
{
    public static EntryDirection NormalBalanceSide(this AccountType type) => type switch
    {
        AccountType.Asset or AccountType.Expense => EntryDirection.Debit,
        AccountType.Liability or AccountType.Equity or AccountType.Revenue => EntryDirection.Credit,
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unhandled account type: {type}")
    };
}