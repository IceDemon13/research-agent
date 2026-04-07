using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Sms
{
    public sealed class SendSms : CallActionWithBodyRequestResultBase<object, SendSms.SendSmsRequest>
    {
        public SendSms(string phone, string smsText, string viberText, int orderId, int? serviceRequestId, int typeId)
            : base(new SendSmsRequest(phone, smsText, viberText, orderId, serviceRequestId, typeId), ApiResources.Sms, "send")
        {
        }

        public class SendSmsRequest
        {
            public SendSmsRequest(string phone, string smsText, string viberText, int orderId, int? serviceRequestId, int typeId)
            {
                Phone = phone;
                SmsText = smsText;
                ViberText = viberText;
                OrderId = orderId;
                ServiceRequestId = serviceRequestId;
                TypeId = typeId;
            }

            [JsonProperty("phone")]
            public string Phone { get; set; }

            [JsonProperty("sms_text")]
            public string SmsText { get; set; }

            [JsonProperty("viber_text")]
            public string ViberText { get; set; }

            [JsonProperty("order_id")]
            public int OrderId { get; set; }

            [JsonProperty("service_request_id")]
            public int? ServiceRequestId { get; set; }

            [JsonProperty("type_id")]
            public int TypeId { get; set; }
        }
    }
}