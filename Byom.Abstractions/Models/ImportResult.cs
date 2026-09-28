namespace Byom.Abstractions.Models;

public sealed record ImportResult(
    IReadOnlyList<Transaction> Transactions,
    int TotalRows,
    int SkippedRows)
{
    public int ParsedRows => Transactions.Count;

    public static ImportResult Empty { get; } =
        new(Array.Empty<Transaction>(), 0, 0);
}
