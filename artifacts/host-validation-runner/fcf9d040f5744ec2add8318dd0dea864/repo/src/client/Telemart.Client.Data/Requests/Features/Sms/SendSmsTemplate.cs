using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Sms
{
    public class SendSmsTemplate : CallEntityActionWithBodyRequestResultBase<object, SendSmsTemplate.SendSmsTemplateRequest>
    {
        public SendSmsTemplate(int id, string phone, int orderId, int? serviceRequestId, decimal? amount, int? creditPartCount, object data, int typeId, string content = null)
            : base(id, new SendSmsTemplateRequest(id, phone, orderId, serviceRequestId, amount, creditPartCount, data, typeId, content), $"{ApiResources.Sms}/templates", "send")
        {
        }

        public class SendSmsTemplateRequest
        {
            public SendSmsTemplateRequest(int id, string phone, int orderId, int? serviceRequestId, decimal? amount, int? creditPartCount, object data, int typeId, string content)
            {
                Id = id;
                Phone = phone;
                OrderId = orderId;
                ServiceRequestId = serviceRequestId;
                Amount = amount;
                CreditPartCount = creditPartCount;
                Data = data;
                TypeId = typeId;
                Content = content;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("phone")]
            public string Phone { get; set; }

            [JsonProperty("order_id")]
            public int OrderId { get; set; }

            [JsonProperty("service_request_id")]
            public int? ServiceRequestId { get; set; }

            [JsonProperty("amount")]
            public decimal? Amount { get; set; }

            [JsonProperty("credit_part_count")]
            public int? CreditPartCount { get; set; }

            [JsonProperty("data")]
            public object Data { get; set; }

            [JsonProperty("type_id")]
            public int TypeId { get; set; }

            [JsonProperty("content")]
            public string Content { get; set; }
        }
    }
}