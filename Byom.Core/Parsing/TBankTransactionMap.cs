using CsvHelper.Configuration;

namespace Byom.Core.Parsing;

internal sealed class TBankTransactionMap : ClassMap<RawTransaction>
{
    public TBankTransactionMap()
    {
        Map(m => m.AccountName).Name("Имя счёта");
        Map(m => m.CardNumber).Name("Номер карты");
        Map(m => m.OperationDate).Name("Дата операции");
        Map(m => m.OperationAmount).Name("Сумма операции");
        Map(m => m.OperationCurrency).Name("Валюта операции");
        Map(m => m.AmountInAccountCurrency).Name("Сумма в валюте счёта");
        Map(m => m.AccountCurrency).Name("Валюта счёта");
        Map(m => m.Status).Name("Статус");
        Map(m => m.DefaultCategory).Name("Категория по-умолчанию");
        Map(m => m.UserCategory).Name("Ваша категория");
        Map(m => m.Mcc).Name("MCC");
        Map(m => m.Description).Name("Описание");
        Map(m => m.Message).Name("Сообщение");
        Map(m => m.Rounding).Name("Округление");
        Map(m => m.AmountWithRounding).Name("Сумма операции с округлением");
        Map(m => m.Bonuses).Name("Бонусы (включая кэшбэк)");
        Map(m => m.IncludeInAnalytics).Name("Учёт в аналитике");
    }
}
