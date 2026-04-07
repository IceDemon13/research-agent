using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ClientContactDto
    {
        [JsonProperty("date")]
        public DateTime? Date { get; set; }

        [JsonProperty("type")]
        public int Type { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("call_state")]
        public int? CallState { get; set; }

        [JsonProperty("sms_state")]
        public int? SmsState { get; set; }

        [JsonProperty("employee_name")]
        public string EmployeeName { get; set; }

        [JsonProperty("initiated_by")]
        public int InitiatedBy { get; set; }

        [JsonProperty("sms_template_id")]
        public int? SmsTemplateId { get; init; }
    }
}