namespace Mercury.Ledger.Entities;

public class JournalEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Reference { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string? Description { get; init; }
    public PaymentChannel Channel { get; init; }
    
    private readonly List<JournalLine> _lines = [];
    public IReadOnlyList<JournalLine> Lines  => _lines;

    private JournalEntry()
    {
    }

    public static JournalEntry Create(string reference, PaymentChannel channel, List<JournalLine> lines,
        string? description = null)
    {
        if (lines.Count < 2) throw new ArgumentException("A journal entry needs at least 2 lines");

        var debits = lines.Where(l => l.Direction == EntryDirection.Debit).Sum(l => l.Amount);
        var credits = lines.Where(l => l.Direction == EntryDirection.Credit).Sum(l => l.Amount);

        if (debits != credits)
        {
            throw new InvalidOperationException($"Journal entry is not balanced: debits {debits} != credits {credits}.");
        };

        var entry = new JournalEntry
        {
            Reference = reference,
            Channel = channel,
            Description = description
        };
        entry._lines.AddRange(lines);
        return entry;
    }
}