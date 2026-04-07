using System.Collections.Generic;
using SmartFormat;
using Telemart.Client.Dictionaries;

namespace Telemart.Client.Business.SmsTemplates.Order
{
    public sealed class LiqpayPrepaymentSmsTemplate : ISmsTemplate
    {
        public LiqpayPrepaymentSmsTemplate(IDictionaries dictionaries, int orderPaymentId, int orderId, int? legalEntityId, int orderStateId, bool dontAllowSentPrepaymentSms)
        {
            TemplateId = SmsTemplate.LiqPayOrderPrepaymentId;

            SmsTemplate template = dictionaries.GetItemById<SmsTemplate>(TemplateId.Value);

            DisplayName = template.Name;

            Data = new { OrderId = orderId, Link = "[ссылка]", PrepaymentAmount = "[сумма]" };

            SmsText = Smart.Format(template.SmsText, Data);
            ViberText = Smart.Format(template.ViberText, Data);

            OrderPaymentId = orderPaymentId;
            OrderStateId = orderStateId;
            LegalEntityId = legalEntityId;
            DontAllowSendPrepaymentSms = dontAllowSentPrepaymentSms;
        }

        public object Data { get; }

        public int? TemplateId { get; }

        public string DisplayName { get; }

        public string SmsText { get; }

        public string ViberText { get; }

        private int OrderPaymentId { get; }

        private int OrderStateId { get; }

        private int? LegalEntityId { get; }

        private bool DontAllowSendPrepaymentSms { get; }

        public IEnumerable<string> Validate()
        {
            if (DontAllowSendPrepaymentSms
                || OrderStateId != OrderStatus.Received.Id)
            {
                yield return "Шаблон не доступен для текущего заказа";
            }

            if (OrderPaymentId != Payment.CashId)
            {
               yield return "Способ оплаты заказа должен быть 'Наличные'";
            }
        }
    }
}