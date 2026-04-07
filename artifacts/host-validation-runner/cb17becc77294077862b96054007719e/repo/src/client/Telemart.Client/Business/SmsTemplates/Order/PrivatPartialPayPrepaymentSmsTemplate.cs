using System.Collections.Generic;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.Order
{
    public class PrivatPartialPayPrepaymentSmsTemplate : ISmsTemplate
    {
        public PrivatPartialPayPrepaymentSmsTemplate(
            int orderPaymentId,
            int orderId,
            int orderStateId,
            SmsTemplate template)
        {
            OrderId = orderId;
            DisplayName = template.Name;

            TemplateId = SmsTemplate.PrivatPartialOrderPrepaymentId;

            Data = new { OrderId = orderId, Link = "[ссылка]", PrepaymentAmount = "[сумма]" };

            SmsText = Smart.Format(template.SmsText, Data);
            ViberText = Smart.Format(template.ViberText, Data);

            OrderPaymentId = orderPaymentId;
            OrderStateId = orderStateId;
        }

        public string DisplayName { get; }

        public string SmsText { get; }

        public string ViberText { get; }

        public object Data { get; }

        public int? TemplateId { get; }

        private int OrderPaymentId { get; }

        private int OrderStateId { get; }

        public int OrderId { get; }

        public IEnumerable<string> Validate()
        {
            if (OrderStateId != OrderStatus.Received.Id)
            {
                yield return "Заказ должен быть в статусе 'Принят'";
            }

            if (OrderId <= 0)
            {
                yield return "Сначала сохраните заказ";
            }

            if (OrderPaymentId != Payment.PrivatPartialPayId)
            {
                yield return "Способ оплаты заказа должен быть 'Оплата Частями ПриватБанк'";
            }
        }
    }
}