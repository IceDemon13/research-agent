using Telemart.Client.Dictionaries;

namespace Telemart.Client.ViewModels
{
    public sealed class ResendSmsParameter
    {
        public ResendSmsParameter(string phone, string content, int? smsTemplateId, int orderId, ClientContactType clientContactType)
        {
            Phone = phone;
            Content = content;
            SmsTemplateId = smsTemplateId;
            OrderId = orderId;
            ClientContactType = clientContactType;
        }

        public string Phone { get; }

        public string Content { get; }

        public int? SmsTemplateId { get; }

        public int OrderId { get; }

        public ClientContactType ClientContactType { get; }
    }
}