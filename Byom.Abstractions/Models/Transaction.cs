namespace Byom.Abstractions.Models;
public sealed record Transaction
{
    public DateOnly Date { get; init; }
    public TimeOnly Time { get; init; }       // из "Дата операции"
    public string CardNumber { get; init; } = "";    // "*7478" или ""
    public decimal Amount { get; init; }      // "-460.00"
    public string Currency { get; init; } = "RUB";
    public string Status { get; init; } = "";        // "Ок"
    public string BankCategory { get; init; } = "";  // "Рестораны"
    public string? Mcc { get; init; }                // "5812"
    public string Description { get; init; } = "";   // "Горница"
    public string? Message { get; init; }            // "От Аллы Филипповой"
    public bool IncludeInAnalytics { get; init; }    // "Да"/"Нет"

    // вычисляемые
    public DateTime Timestamp => Date.ToDateTime(Time);
    public bool IsExpense => Amount < 0;
    public bool IsIncome => Amount > 0;
}
