using Mercury.Api.Data;
using Mercury.Ledger.Entities;
using Mercury.Ledger.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Mercury.Tests;

public class LedgerServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly LedgerService _sut; // sut means system under test

    public LedgerServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // must stay open for 'test

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _sut = new LedgerService(_db);
    }


    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

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


    [Fact]
    public async Task PostSaleAsync_Then_GetAccountBalanceAsync_Reflects_The_Sale()
    {
        var pendingAccount = new Account
            { Code = "STORE-B-PENDING", Name = "Store B - Pending Settlement", Type = AccountType.Asset };
        var revenueAccount = new Account
            { Code = "SALES-REVENUE-B", Name = "Sales Revenue - Store B", Type = AccountType.Revenue };


        _db.Set<Account>().AddRange(pendingAccount, revenueAccount);
        await _db.SaveChangesAsync();


        await _sut.PostSaleAsync(
            pendingAccount.Id,
            revenueAccount.Id,
            amount: 2000M,
            reference: "PAYSTACK-TXN-123",
            channel: PaymentChannel.BankTransfer
        );

        var pendingSettlementBalance = await _sut.GetAccountBalanceAsync(pendingAccount.Id);
        var revenueBalance = await _sut.GetAccountBalanceAsync(revenueAccount.Id);

        Assert.Equal(2000M, pendingSettlementBalance);
        Assert.Equal(2000M, revenueBalance);
    }

    [Fact]
    public async Task PostSaleAsync_Then_GetAccountBalancesAsync_Reflects_The_Sale()
    {
        var pendingSettlementAccount = new Account
            { Code = "SXB", Name = "Store A - Pending Settlement", Type = AccountType.Asset };
        var revenueAccount = new Account
            { Code = "SX12", Name = "Store A - Revenue Account", Type = AccountType.Revenue };

        _db.Set<Account>().AddRange(pendingSettlementAccount, revenueAccount);
        await _db.SaveChangesAsync();

        await _sut.PostSaleAsync(
            pendingSettlementAccount.Id,
            revenueAccount.Id,
            reference: "PAYSTACK-TXN-456",
            amount: 1000M,
            channel: PaymentChannel.Cash
        );

        var result = await _sut.GetAccountBalancesAsync([revenueAccount.Id, pendingSettlementAccount.Id]);

        Assert.Equal(2, result.Count);
        Assert.Equal(1000M, result[pendingSettlementAccount.Id]);
        Assert.Equal(1000M, result[revenueAccount.Id]);
    }

    [Fact]
    public async Task PostSaleAsync_Then_Reversal_ZeroesPendingSettlementAndRevenue()
    {
        var pendingSettlementAccount = new Account { Code = "OMO-P-2", Name = "OMO-PENDING-SETTLEMENT", Type = AccountType.Asset};
        var revenueAccount = new Account { Code = "OMO-R-2", Name = "OMO-REVENUE", Type = AccountType.Revenue };
        
        _db.Set<Account>().AddRange(pendingSettlementAccount, revenueAccount);
        await _db.SaveChangesAsync();
        
        var accountBalances = await _sut.GetAccountBalancesAsync([revenueAccount.Id, pendingSettlementAccount.Id]);
        Assert.Equal(0M, accountBalances[pendingSettlementAccount.Id]);
        Assert.Equal(0M, accountBalances[revenueAccount.Id]);
    }

    [Fact]
    public async Task PostSaleAsync_ThenFullRefund_ZeroesPendingSettlementAndPreservesRevenue()
    {
        var pendingSettlementAccount = new Account
            { Code = "MR-P-SETTLEMENT", Name = "Pending Settlement - Store P", Type = AccountType.Asset };
        var revenueAccount = new Account
            { Code = "MR-P-REVENUE", Name = "Revenue Account - Store P", Type = AccountType.Revenue };
        var refundAccount = new Account
            { Code = "MR-P-REFUNDS", Name = "Refunds Account - Store P", Type = AccountType.Expense };

        _db.Set<Account>().AddRange(pendingSettlementAccount, revenueAccount, refundAccount);
        await _db.SaveChangesAsync();

        await _sut.PostSaleAsync(
            pendingSettlementAccount.Id,
            revenueAccount.Id,
            reference: "PAYSTACK-TXN-939",
            channel: PaymentChannel.PosCard,
            amount: 1000M
        );

        await _sut.PostRefundAsync(
            refundAccount.Id,
            pendingSettlementAccount.Id,
            amount: 1000M,
            channel: PaymentChannel.BankTransfer,
            reference: "PAYSTACK-TXN-940"
        );


        var accountBalances =
            await _sut.GetAccountBalancesAsync([revenueAccount.Id, pendingSettlementAccount.Id, refundAccount.Id]);
        
        Assert.Equal(0M, accountBalances[pendingSettlementAccount.Id]);
        Assert.Equal(1000M, accountBalances[refundAccount.Id]);
        Assert.Equal(1000M, accountBalances[revenueAccount.Id]);
    }
}