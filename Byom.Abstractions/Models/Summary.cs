namespace Byom.Abstractions.Models;

public sealed record CategorySummary(
    string Category,
    decimal Total,
    int Count,
    double Share)
{
    /// <summary>Доля в процентах (0..100).</summary>
    public double SharePercent => Share * 100.0;
}

public sealed record MonthlySummary(
    int Year,
    int Month,
    decimal Income,
    decimal Expense,
    decimal Net)
{
    /// <summary>Человекочитаемое имя месяца: "Сентябрь 2026".</summary>
    public string DisplayName
    {
        get
        {
            var raw = new DateOnly(Year, Month, 1)
                .ToString("MMMM yyyy", new System.Globalization.CultureInfo("ru-RU"));

            if (string.IsNullOrEmpty(raw))
            {
                return raw;
            }

            return char.ToUpper(raw[0], new System.Globalization.CultureInfo("ru-RU")) + raw[1..];
        }
    }
}

public sealed record PeriodSummary(
    DateOnly From,
    DateOnly To,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Net,
    decimal AverageExpense,
    int ExpenseCount,
    int IncomeCount,
    IReadOnlyList<CategorySummary> Categories,
    IReadOnlyList<MonthlySummary> Months)
{
    public static PeriodSummary Empty { get; } = new(
        DateOnly.MinValue,
        DateOnly.MinValue,
        0m, 0m, 0m, 0m,
        0, 0,
        Array.Empty<CategorySummary>(),
        Array.Empty<MonthlySummary>());

    public bool HasData => Categories.Count > 0 || Months.Count > 0;

    public CategorySummary? TopCategory => Categories.Count > 0 ? Categories[0] : null;
}
