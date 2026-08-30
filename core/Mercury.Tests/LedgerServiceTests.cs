using Mercury.Ledger.Entities;

namespace Mercury.Tests;

public class LedgerServiceTests
{
    [Fact]
    public void Create_Throws_When_Debits_And_Credits_Are_Unequal()
    {
        var act = () => JournalEntry.Create(
            reference: "TEST 1",
            channel: PaymentChannel.BankTransfer,
            lines:
            [
                new JournalLine { AccountId = Guid.NewGuid(), Direction = EntryDirection.Debit, Amount = 100 },
                new JournalLine { AccountId = Guid.NewGuid(), Direction = EntryDirection.Credit, Amount = 90 }
            ]
        );

        Assert.Throws<InvalidOperationException>(act);
    }


    [Fact]
    public void Create_Succeeds_When_Balanced_Across_Multiple_Lines()
    {
        var entry = JournalEntry.Create(
            reference: "TEST 2",
            channel: PaymentChannel.BankTransfer,
            lines:
            [
                new JournalLine { AccountId = Guid.NewGuid(), Direction = EntryDirection.Debit, Amount = 1970 },
                new JournalLine { AccountId = Guid.NewGuid(), Direction = EntryDirection.Debit, Amount = 30 },
                new JournalLine { AccountId = Guid.NewGuid(), Direction = EntryDirection.Credit, Amount = 2000 }
            ]);
        
        Assert.Equal(3, entry.Lines.Count);
        Assert.Equal("TEST 2", entry.Reference);
        Assert.Equal(2000M, entry.Lines.Where(l => l.Direction == EntryDirection.Credit).Sum(l => l.Amount));
        Assert.Equal(2000M, entry.Lines.Where(l => l.Direction == EntryDirection.Debit).Sum(l => l.Amount));
        Assert.Equal(PaymentChannel.BankTransfer, entry.Channel);
        
    }
}