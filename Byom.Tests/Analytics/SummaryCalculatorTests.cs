using Byom.Abstractions.Models;
using Byom.Core.Analytics;
using FluentAssertions;
using Xunit;

namespace Byom.Tests.Analytics;

public sealed class SummaryCalculatorTests
{
    private readonly SummaryCalculator _sut = new();

    [Fact]
    public void Calculate_EmptyCollection_ReturnsEmptySummary()
    {
        var result = _sut.Calculate(Array.Empty<Transaction>());

        result.HasData.Should().BeFalse();
        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.Categories.Should().BeEmpty();
        result.Months.Should().BeEmpty();
    }

    [Fact]
    public void Calculate_SingleExpense_ComputesTotals()
    {
        var transactions = new[]
        {
            MakeTx(new DateOnly(2026, 9, 1), -100m, "Супермаркеты")
        };

        var result = _sut.Calculate(transactions);

        result.TotalExpense.Should().Be(100m);
        result.TotalIncome.Should().Be(0m);
        result.Net.Should().Be(-100m);
        result.AverageExpense.Should().Be(100m);
        result.ExpenseCount.Should().Be(1);
    }

    [Fact]
    public void Calculate_IncomeAndExpense_ComputesNet()
    {
        var transactions = new[]
        {
            MakeTx(new DateOnly(2026, 9, 1), 1000m, "Зарплата"),
            MakeTx(new DateOnly(2026, 9, 2), -300m, "Супермаркеты"),
            MakeTx(new DateOnly(2026, 9, 3), -200m, "Кафе"),
        };

        var result = _sut.Calculate(transactions);

        result.TotalIncome.Should().Be(1000m);
        result.TotalExpense.Should().Be(500m);
        result.Net.Should().Be(500m);
        result.AverageExpense.Should().Be(250m);
    }

    [Fact]
    public void Calculate_GroupsByCategory_SortedByTotalDescending()
    {
        var transactions = new[]
        {
            MakeTx(new DateOnly(2026, 9, 1), -100m, "Кафе"),
            MakeTx(new DateOnly(2026, 9, 2), -500m, "Супермаркеты"),
            MakeTx(new DateOnly(2026, 9, 3), -300m, "Супермаркеты"),
            MakeTx(new DateOnly(2026, 9, 4), -200m, "Кафе"),
        };

        var result = _sut.Calculate(transactions);

        result.Categories.Should().HaveCount(2);
        result.Categories[0].Category.Should().Be("Супермаркеты");
        result.Categories[0].Total.Should().Be(800m);
        result.Categories[0].Count.Should().Be(2);
        result.Categories[1].Category.Should().Be("Кафе");
        result.Categories[1].Total.Should().Be(300m);
    }

    [Fact]
    public void Calculate_Shares_SumTo100Percent()
    {
        var transactions = new[]
        {
            MakeTx(new DateOnly(2026, 9, 1), -500m, "Супермаркеты"),
            MakeTx(new DateOnly(2026, 9, 2), -300m, "Кафе"),
            MakeTx(new DateOnly(2026, 9, 3), -200m, "Такси"),
        };

        var result = _sut.Calculate(transactions);

        var totalShare = result.Categories.Sum(c => c.Share);
        totalShare.Should().BeApproximately(1.0, 1e-9);
        result.Categories[0].SharePercent.Should().BeApproximately(50.0, 1e-6);
    }

    [Fact]
    public void Calculate_EmptyCategory_UsesFallbackLabel()
    {
        var transactions = new[]
        {
            MakeTx(new DateOnly(2026, 9, 1), -100m, "")
        };

        var result = _sut.Calculate(transactions);

        result.Categories.Should().ContainSingle();
        result.Categories[0].Category.Should().Be("Без категории");
    }

    [Fact]
    public void Calculate_GroupsByMonth_SortedChronologically()
    {
        var transactions = new[]
        {
            MakeTx(new DateOnly(2026, 9, 15), -100m, "Кафе"),
            MakeTx(new DateOnly(2026, 8, 15), -200m, "Кафе"),
            MakeTx(new DateOnly(2026, 9, 20), -50m, "Кафе"),
        };

        var result = _sut.Calculate(transactions);

        result.Months.Should().HaveCount(2);
        result.Months[0].Month.Should().Be(8);
        result.Months[0].Expense.Should().Be(200m);
        result.Months[1].Month.Should().Be(9);
        result.Months[1].Expense.Should().Be(150m);
        result.Months[1].Net.Should().Be(-150m);
    }

    [Fact]
    public void Calculate_TopCategory_ReturnsLargest()
    {
        var transactions = new[]
        {
            MakeTx(new DateOnly(2026, 9, 1), -100m, "Кафе"),
            MakeTx(new DateOnly(2026, 9, 2), -500m, "Супермаркеты"),
        };

        var result = _sut.Calculate(transactions);

        result.TopCategory.Should().NotBeNull();
        result.TopCategory!.Category.Should().Be("Супермаркеты");
    }

    [Fact]
    public void Calculate_MonthlyDisplayName_IsRussian()
    {
        var transactions = new[]
        {
            MakeTx(new DateOnly(2026, 9, 15), -100m, "Кафе"),
        };

        var result = _sut.Calculate(transactions);

        result.Months[0].DisplayName.Should().StartWith("Сентябрь");
        result.Months[0].DisplayName.Should().Contain("2026");
    }

    private static Transaction MakeTx(DateOnly date, decimal amount, string category)
        => new()
        {
            Date = date,
            Time = new TimeOnly(12, 0),
            Amount = amount,
            BankCategory = category,
            Status = "Ок",
            IncludeInAnalytics = true,
        };
}
