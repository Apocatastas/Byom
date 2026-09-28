using Byom.Abstractions.Interfaces;
using Byom.Abstractions.Models;

namespace Byom.Core.Analytics;

public sealed class SummaryCalculator : ISummaryCalculator
{
    private const string Uncategorized = "Без категории";

    public PeriodSummary Calculate(IReadOnlyCollection<Transaction> transactions)
    {
        if (transactions is null || transactions.Count == 0)
        {
            return PeriodSummary.Empty;
        }

        var expenses = transactions.Where(t => t.IsExpense).ToList();
        var incomes = transactions.Where(t => t.IsIncome).ToList();

        var totalIncome = incomes.Sum(t => t.Amount);
        var totalExpense = Math.Abs(expenses.Sum(t => t.Amount));
        var net = totalIncome - totalExpense;
        var avgExpense = expenses.Count > 0
            ? totalExpense / expenses.Count
            : 0m;

        var categories = BuildCategories(expenses, totalExpense);
        var months = BuildMonths(transactions);

        var from = transactions.Min(t => t.Date);
        var to = transactions.Max(t => t.Date);

        return new PeriodSummary(
            From: from,
            To: to,
            TotalIncome: totalIncome,
            TotalExpense: totalExpense,
            Net: net,
            AverageExpense: avgExpense,
            ExpenseCount: expenses.Count,
            IncomeCount: incomes.Count,
            Categories: categories,
            Months: months);
    }

    private static IReadOnlyList<CategorySummary> BuildCategories(
        List<Transaction> expenses,
        decimal totalExpense)
    {
        if (expenses.Count == 0)
        {
            return Array.Empty<CategorySummary>();
        }

        return expenses
            .GroupBy(t => string.IsNullOrWhiteSpace(t.BankCategory)
                ? Uncategorized
                : t.BankCategory)
            .Select(g =>
            {
                var total = Math.Abs(g.Sum(t => t.Amount));
                var share = totalExpense > 0
                    ? (double)(total / totalExpense)
                    : 0.0;

                return new CategorySummary(
                    Category: g.Key,
                    Total: total,
                    Count: g.Count(),
                    Share: share);
            })
            .OrderByDescending(c => c.Total)
            .ToList();
    }

    private static IReadOnlyList<MonthlySummary> BuildMonths(
        IReadOnlyCollection<Transaction> transactions)
    {
        return transactions
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g =>
            {
                var income = g.Where(t => t.IsIncome).Sum(t => t.Amount);
                var expense = Math.Abs(g.Where(t => t.IsExpense).Sum(t => t.Amount));
                return new MonthlySummary(
                    Year: g.Key.Year,
                    Month: g.Key.Month,
                    Income: income,
                    Expense: expense,
                    Net: income - expense);
            })
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToList();
    }
}
