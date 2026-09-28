using Byom.Abstractions.Models;

namespace Byom.Abstractions.Interfaces;
public interface ITransactionParser
{
    Task<ImportResult> ParseAsync(
        string path,
        CancellationToken ct);
}
