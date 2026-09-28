using Byom.Abstractions.Interfaces;
using Byom.Abstractions.Models;

namespace Byom.Core.Parsing;

public sealed class TBankCsvParser : ITransactionParser
{
    public Task<ImportResult> ParseAsync(string path, CancellationToken ct)
    {
        // TODO: настоящая реализация через CsvHelper (задача 1.7)
        var empty = new ImportResult(
            Transactions: Array.Empty<Transaction>(),
            TotalRows: 0,
            SkippedRows: 0);


          return Task.FromResult(ImportResult.Empty);
    }
}
