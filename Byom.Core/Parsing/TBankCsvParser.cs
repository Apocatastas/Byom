using System.Globalization;
using System.Runtime.CompilerServices;
using Byom.Abstractions.Interfaces;
using Byom.Abstractions.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace Byom.Core.Parsing;

public sealed class TBankCsvParser : ITransactionParser
{
    private static readonly CultureInfo RussianCulture = new("ru-RU");
    private static readonly string[] DateFormats =
    {
        "dd.MM.yyyy HH:mm:ss",
        "dd.MM.yyyy HH:mm",
        "dd.MM.yyyy"
    };

    private readonly ILogger<TBankCsvParser> _logger;

    public TBankCsvParser(ILogger<TBankCsvParser> logger)
    {
        _logger = logger;
    }

    public async Task<ImportResult> ParseAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл не найден", filePath);
        }

        var config = new CsvConfiguration(RussianCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null,           // не падать на битых строках
            MissingFieldFound = null,      // не падать на отсутствующих колонках
            HeaderValidated = null,        // не падать, если не все заголовки совпали
            DetectColumnCountChanges = false
        };

        var transactions = new List<Transaction>();
        var totalRows = 0;
        var skippedRows = 0;

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<TBankTransactionMap>();

        // Читаем в фоне, чтобы не блокировать UI-поток
        await Task.Run(async () =>
        {
            var records = csv.GetRecordsAsync<RawTransaction>(ct);

            await foreach (var raw in records.WithCancellation(ct))
            {
                totalRows++;

                if (!ShouldInclude(raw, out var reason))
                {
                    skippedRows++;
                    _logger.LogTrace("Строка {Row} пропущена: {Reason}", totalRows, reason);
                    continue;
                }

                var tx = MapToTransaction(raw);
                if (tx is null)
                {
                    skippedRows++;
                    _logger.LogWarning(
                        "Строка {Row} не смапилась: дата='{Date}', сумма='{Amount}'",
                        totalRows, raw.OperationDate, raw.OperationAmount);
                    continue;
                }

                transactions.Add(tx);
            }
        }, ct);

        _logger.LogInformation(
            "Файл разобран: всего {Total}, загружено {Parsed}, пропущено {Skipped}",
            totalRows, transactions.Count, skippedRows);

        return new ImportResult(transactions, totalRows, skippedRows);
    }

    private static bool ShouldInclude(RawTransaction raw, out string reason)
    {
        if (!string.Equals(raw.Status, "Ок", StringComparison.OrdinalIgnoreCase))
        {
            reason = $"статус '{raw.Status}'";
            return false;
        }

        if (!string.Equals(raw.IncludeInAnalytics, "Да", StringComparison.OrdinalIgnoreCase))
        {
            reason = $"учёт в аналитике '{raw.IncludeInAnalytics}'";
            return false;
        }

        reason = "";
        return true;
    }

    private static Transaction? MapToTransaction(RawTransaction raw)
    {
        if (!TryParseDateTime(raw.OperationDate, out var date, out var time))
        {
            return null;
        }

        if (!TryParseAmount(raw.OperationAmount, out var amount))
        {
            return null;
        }

        return new Transaction
        {
            Date = date,
            Time = time,
            CardNumber = raw.CardNumber,
            Amount = amount,
            Currency = string.IsNullOrWhiteSpace(raw.OperationCurrency) ? "RUB" : raw.OperationCurrency,
            Status = raw.Status,
            BankCategory = raw.DefaultCategory,
            Mcc = string.IsNullOrWhiteSpace(raw.Mcc) ? null : raw.Mcc,
            Description = raw.Description,
            Message = string.IsNullOrWhiteSpace(raw.Message) ? null : raw.Message,
            IncludeInAnalytics = true,   // мы уже отфильтровали в ShouldInclude
        };
    }

    private static bool TryParseDateTime(string raw, out DateOnly date, out TimeOnly time)
    {
        date = default;
        time = default;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (!DateTime.TryParseExact(
                raw,
                DateFormats,
                RussianCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return false;
        }

        date = DateOnly.FromDateTime(parsed);
        time = TimeOnly.FromDateTime(parsed);
        return true;
    }

    private static bool TryParseAmount(string raw, out decimal amount)
    {
        amount = 0m;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        // "-460,00" -> -460.00
        return decimal.TryParse(
            raw,
            NumberStyles.Number | NumberStyles.AllowLeadingSign,
            RussianCulture,
            out amount);
    }
}
