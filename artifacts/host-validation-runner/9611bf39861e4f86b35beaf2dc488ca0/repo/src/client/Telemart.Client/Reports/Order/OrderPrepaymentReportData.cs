using System;
using System.Globalization;
using Humanizer;
using Telemart.Client.Business;
using Telemart.Client.Core.Helpers;
using Currency = Telemart.Client.Dictionaries.Currency;

namespace Telemart.Client.Reports.Order
{
    public class OrderPrepaymentReportData
    {
        private readonly CultureInfo ruCulture = new CultureInfo("ru-RU");

        public OrderPrepaymentReportData(
            int orderId,
            string orderFio,
            DateTime orderCreatedOn,
            int orderPaymentId,
            Currency orderPaymentCurrency,
            decimal orderPaymentAmount,
            DateTime orderPaymentCreatedOn,
            string orderPaymentCreatedByName)
        {
            Header = $"Квитанция к приходному ордеру №{orderPaymentId} от {orderPaymentCreatedOn.ToString("dd MMMM yyyy", ruCulture)} г.";
            Authority = $"Заказ №{orderId} от {orderCreatedOn:dd.MM.yyyy}";
            PrepaymentCreatedByName = orderPaymentCreatedByName;
            Fio = orderFio;
            SummStr = $"{CurrencyFormatingRules.ToStr(orderPaymentAmount, orderPaymentCurrency.Id, "C2")} ({DecimalToWords(orderPaymentAmount, orderPaymentCurrency)})";
        }

        public string Header { get; }

        public string Authority { get; }

        public string PrepaymentCreatedByName { get; }

        public string Fio { get; }

        public string SummStr { get; }

        private string DecimalToWords(decimal value, Currency currency)
        {
            int integralPart = (int)Math.Truncate(value);
            int floatPart = (int)((value - integralPart) * 100);

            string[] integralPartWords;
            string[] floatPartWords;

            if (currency == Currency.Usd)
            {
                integralPartWords = new[] { "доллар", "доллара", "долларов" };
                floatPartWords = new[] { "цент", "цента", "центов" };
            }
            else if (currency == Currency.Uah)
            {
                integralPartWords = new[] { "гривна", "гривны", "гривен" };
                floatPartWords = new[] { "копейка", "копейки", "копеек" };
            }
            else if (currency == Currency.Eur)
            {
                integralPartWords = new[] { "евро", "евро", "евро" };
                floatPartWords = new[] { "евроцент", "евроцента", "евроцентов" };
            }
            else
            {
                throw new NotSupportedException("Currency not supported");
            }

            string currencyWord = integralPart == 0
                ? integralPartWords[2]
                : WordEndingHelper.GetWordByNumber(integralPart, integralPartWords);
            string currencyFloatPartWord = floatPart == 0
                ? floatPartWords[2]
                : WordEndingHelper.GetWordByNumber(floatPart, floatPartWords);

            return $"{integralPart.ToWords(GrammaticalGender.Feminine, ruCulture)} {currencyWord} {floatPart:D2} {currencyFloatPartWord}".Transform(To.SentenceCase);
        }
    }
}