using Byom.Core.Parsing;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Byom.Tests.Parsing;

public sealed class TBankCsvParserTests
{
    private static TBankCsvParser CreateParser()
        => new(NullLogger<TBankCsvParser>.Instance);

    private static string TestFilePath(string fileName)
        => Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

    [Fact]
    public async Task ParseAsync_ValidFile_ReturnsTransactions()
    {
        // Arrange
        var parser = CreateParser();
        var path = TestFilePath("tbank-sample.csv");

        // Act
        var result = await parser.ParseAsync(path);

        // Assert
        result.Transactions.Should().NotBeEmpty();
        result.TotalRows.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ParseAsync_ParsesNegativeAmountWithComma()
    {
        var parser = CreateParser();
        var path = TestFilePath("tbank-sample.csv");

        var result = await parser.ParseAsync(path);

        var expense = result.Transactions.First(t => t.Amount < 0);
        expense.IsExpense.Should().BeTrue();
        expense.Amount.Should().BeLessThan(0m);
    }

    [Fact]
    public async Task ParseAsync_SkipsNonAnalyticsRows()
    {
        var parser = CreateParser();
        var path = TestFilePath("tbank-sample.csv");

        var result = await parser.ParseAsync(path);

        // В файле есть строки с "Учёт в аналитике = Нет"
        result.SkippedRows.Should().BeGreaterThan(0);
        result.Transactions.Should().OnlyContain(t => t.IncludeInAnalytics);
    }

    [Fact]
    public async Task ParseAsync_SkipsNonOkStatus()
    {
        var parser = CreateParser();
        var path = TestFilePath("tbank-sample.csv");

        var result = await parser.ParseAsync(path);

        result.Transactions.Should().OnlyContain(t => t.Status == "Ок");
    }

    [Fact]
    public async Task ParseAsync_FileNotFound_Throws()
    {
        var parser = CreateParser();

        var act = async () => await parser.ParseAsync("nonexistent.csv");

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task ParseAsync_CancellationRequested_Throws()
    {
        var parser = CreateParser();
        var path = TestFilePath("tbank-sample.csv");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await parser.ParseAsync(path, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
