using Mercury.Ledger.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercury.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options): DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Code).IsUnique();
            entity.Property(a => a.Code).HasMaxLength(64).IsRequired();
            entity.Property(a => a.Name).HasMaxLength(128).IsRequired();
            entity.Property(a => a.Type).HasMaxLength(32).IsRequired();
            
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
            entity.Property(l => l.Amount).HasPrecision(18, 2);   // edge case #4 — mandatory, not optional

            entity.HasOne<Account>()
                .WithMany()
                .HasForeignKey(l => l.AccountId)
                .OnDelete(DeleteBehavior.Restrict);       // edge case #5
        });
    }
}