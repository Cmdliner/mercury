namespace Mercury.Ledger.Entities;

public enum PaymentChannel
{
    Cash,
    BankTransfer,
    PosCard
}

public enum EntryDirection {
    Debit,
    Credit
}

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}