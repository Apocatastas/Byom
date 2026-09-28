using Byom.Abstractions.Models;

namespace Byom.Abstractions.Interfaces;

public interface ISummaryCalculator
{
    PeriodSummary Calculate(IReadOnlyCollection<Transaction> transactions);
}
