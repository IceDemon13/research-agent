using System.Collections.Generic;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.Order
{
    public sealed class RequisitesSmsTemplate : ISmsTemplate
    {
        public RequisitesSmsTemplate(int orderId, Payment paymentType, decimal toPayUah, SmsTemplate template)
        {
            OrderId = orderId;
            PaymentType = paymentType;

            TemplateId = SmsTemplate.OrderNotWhitePaymentId;

            DisplayName = template.Name;

            Data = new { OrderId = orderId, AmountStr = CurrencyFormatingRules.ToUahStr(toPayUah, currencySeparetor: string.Empty) };

            SmsText = Smart.Format(template.SmsText, Data);
            ViberText = Smart.Format(template.ViberText, Data);
        }

        public int OrderId { get; }

        public object Data { get; }

        public int? TemplateId { get; }

        public Payment PaymentType { get; }

        public string DisplayName { get; }

        public string SmsText { get; }

        public string ViberText { get; }

        public IEnumerable<string> Validate()
        {
            if (OrderId <= 0)
            {
                yield return "Сначала сохраните заказ";
            }
        }
    }
}