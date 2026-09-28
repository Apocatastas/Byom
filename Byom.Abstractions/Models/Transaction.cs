namespace Byom.Abstractions.Models;

public sealed record Transaction
{
    public DateOnly Date { get; init; }
    public TimeOnly Time { get; init; }
    public string CardNumber { get; init; } = "";
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "RUB";
    public string Status { get; init; } = "";
    public string BankCategory { get; init; } = "";
    public string? Mcc { get; init; }
    public string Description { get; init; } = "";
    public string? Message { get; init; }
    public bool IncludeInAnalytics { get; init; }

    public bool IsExpense => Amount < 0;
    public bool IsIncome => Amount > 0;
}
