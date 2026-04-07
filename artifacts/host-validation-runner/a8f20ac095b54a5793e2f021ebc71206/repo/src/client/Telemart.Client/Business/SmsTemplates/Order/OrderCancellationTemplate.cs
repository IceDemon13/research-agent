using System.Collections.Generic;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.Order
{
    public sealed class OrderCancellationTemplate : ISmsTemplate
    {
        public OrderCancellationTemplate(int orderId, SmsTemplate template)
        {
            OrderId = orderId;

            DisplayName = template.Name;

            TemplateId = SmsTemplate.OrderCancelId;

            Data = new { OrderId = orderId };

            SmsText = Smart.Format(template.SmsText, Data);
            ViberText = Smart.Format(template.ViberText, Data);
        }

        public string DisplayName { get; }

        public object Data { get; }

        public int? TemplateId { get; }

        public int OrderId { get; }

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