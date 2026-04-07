using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Complaint
{
    public class ComplaintCreateDto
    {
        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("source_id")]
        public int SourceId { get; set; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; set; }

        [JsonProperty("order_id")]
        public int? OrderId { get; set; }

        [JsonProperty("service_request_id")]
        public int? ServiceRequestId { get; set; }

        [JsonProperty("trade_in_id")]
        public int? TradeInId { get; set; }

        [JsonProperty("contractor_id")]
        public int? ContractorId { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("employee_id")]
        public int EmployeeId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("deadline")]
        public DateTime Deadline { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}