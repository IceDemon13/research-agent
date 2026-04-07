using System.Collections.Generic;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.Order
{
    public sealed class CouldNotContactSmsTemplate : ISmsTemplate
    {
        public CouldNotContactSmsTemplate(int orderId, Subdivision subdivision, SmsTemplate template)
        {
            OrderId = orderId;
            Subdivision = subdivision;

            TemplateId = SmsTemplate.CouldNotContactId;

            DisplayName = template.Name;

            Data = new { OrderId = orderId };

            SmsText = Smart.Format(template.SmsText, Data);
            ViberText = Smart.Format(template.ViberText, Data);
        }

        public int OrderId { get; }

        public string ViberText { get; }

        public object Data { get; }

        public int? TemplateId { get; }

        public Subdivision Subdivision { get; }

        public string DisplayName { get; }

        public string SmsText { get; }

        public IEnumerable<string> Validate()
        {
            if (OrderId <= 0)
            {
                yield return "Сначала сохраните заказ";
            }

            if (Subdivision != Subdivision.Telemart && Subdivision != Subdivision.Nofelet)
            {
                yield return $"Шаблон доступен только для подразделений \"{Subdivision.Telemart.Name}\" и \"{Subdivision.Nofelet.Name}\"";
            }
        }
    }
}