using Mercury.Ledger.Entities;
using Mercury.Merchants.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Mercury.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();

    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Staff> StaffMembers => Set<Staff>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Code).IsUnique();
            entity.Property(a => a.Code).HasMaxLength(64).IsRequired();
            entity.Property(a => a.Name).HasMaxLength(128).IsRequired();
            entity.Property(a => a.Type).HasConversion<string>().HasMaxLength(16).IsRequired();


            entity.HasData(
                new Account
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Code = "STORE-A-PENDING",
                    Name = "Store A – Pending Settlement", Type = AccountType.Asset
                },
                new Account
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Code = "STORE-A-CASH-TILL",
                    Name = "Store A – Cash Till", Type = AccountType.Asset
                },
                new Account
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Code = "PHARMACY-BANK",
                    Name = "Pharmacy Bank Account", Type = AccountType.Asset
                },
                new Account
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Code = "SALES-REVENUE-A",
                    Name = "Sales Revenue – Store A", Type = AccountType.Revenue
                },
                new Account
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Code = "REFUNDS-A",
                    Name = "Refunds – Store A", Type = AccountType.Expense
                }
            );
        });
        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.HasIndex(j => j.Reference);
            entity.Property(j => j.Reference).HasMaxLength(128).IsRequired();
            entity.Property(j => j.Channel).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(j => j.Description).HasMaxLength(512);

            // Lines is IReadOnlyList backed by a private field — EF needs to be told
            // explicitly to use the backing field, since there's no public setter.
            entity.Metadata.FindNavigation(nameof(JournalEntry.Lines))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(j => j.Lines)
                .WithOne()
                .HasForeignKey(l => l.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict); // never cascade delete
        });

        modelBuilder.Entity<JournalLine>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => l.AccountId);
            entity.Property(l => l.Direction).HasConversion<string>().HasMaxLength(8);
            entity.Property(l => l.Amount).HasPrecision(18, 2); // edge case #4 — mandatory, not optional

            entity.HasOne<Account>()
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict); // edge case #5
        });


        // Merchant 
        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Name).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(128).IsRequired();
            entity.Property(s => s.Location).HasMaxLength(128).IsRequired();
            entity.HasOne<Merchant>().WithMany(m => m.Stores).HasForeignKey(s => s.MerchantId);
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(128).IsRequired();
            entity.Property(s => s.Role).HasConversion<string>().HasMaxLength(32);
            entity.HasOne<Merchant>().WithMany(m => m.StaffMembers).HasForeignKey(s => s.MerchantId);
            entity.HasOne<Store>().WithMany(s => s.StaffMembers).HasForeignKey(s => s.StoreId).IsRequired(false);
            
            entity.HasOne<IdentityUser<Guid>>()
                .WithMany()
                .HasForeignKey((s => s.IdentityUserId))
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}