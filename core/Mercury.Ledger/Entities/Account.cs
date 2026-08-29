namespace Mercury.Ledger.Entities;

public class Account
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required string Type { get; init; }
}