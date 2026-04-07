using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Complaint
{
    public class ComplaintDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

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

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("employee_id")]
        public int? EmployeeId { get; set; }

        [JsonProperty("bitrix_id")]
        public int? BitrixId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock_name")]
        public string EmployeeLockName { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("resolution")]
        public string Resolution { get; set; }

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

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; set; }

        [JsonProperty("completed_by")]
        public int? CompletedBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }
    }
}