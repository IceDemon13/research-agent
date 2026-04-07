using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Call
{
    public class CallCreateDto
    {
        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("contractor_id")]
        public int? ContractorId { get; set; }

        [JsonProperty("order_id")]
        public int? OrderId { get; set; }

        [JsonProperty("service_request_id")]
        public int? ServiceRequestId { get; set; }

        [JsonProperty("call_type_id")]
        public int CallTypeId { get; set; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; set; }

        [JsonProperty("responsible_employee_id")]
        public int? ResponsibleEmployeeId { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("call_from")]
        public DateTime? CallFrom { get; set; }

        [JsonProperty("call_to")]
        public DateTime? CallTo { get; set; }

        [JsonProperty("task")]
        public string Task { get; set; }

        [JsonProperty("incoming")]
        public bool Incoming { get; set; }
    }
}