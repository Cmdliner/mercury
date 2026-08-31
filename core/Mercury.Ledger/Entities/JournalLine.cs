namespace Mercury.Ledger.Entities;

public class JournalLine
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid JournalEntryId { get; init; }
    public Guid AccountId { get; init; }
    public EntryDirection Direction { get; init; }
    public decimal Amount { get; init; } // should always be a positive value ..a udecimal kindof entry direction says if its a credit or debit 
    
}