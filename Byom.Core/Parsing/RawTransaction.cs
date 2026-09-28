namespace Byom.Core.Parsing;

internal sealed class RawTransaction
{
    public string AccountName { get; set; } = "";
    public string CardNumber { get; set; } = "";
    public string OperationDate { get; set; } = "";
    public string OperationAmount { get; set; } = "";
    public string OperationCurrency { get; set; } = "";
    public string AmountInAccountCurrency { get; set; } = "";
    public string AccountCurrency { get; set; } = "";
    public string Status { get; set; } = "";
    public string DefaultCategory { get; set; } = "";
    public string UserCategory { get; set; } = "";
    public string Mcc { get; set; } = "";
    public string Description { get; set; } = "";
    public string Message { get; set; } = "";
    public string Rounding { get; set; } = "";
    public string AmountWithRounding { get; set; } = "";
    public string Bonuses { get; set; } = "";
    public string IncludeInAnalytics { get; set; } = "";
}
