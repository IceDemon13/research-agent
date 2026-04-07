using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Call
{
    public class CallDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("call_type_id")]
        public int CallTypeId { get; set; }

        [JsonProperty("call_type")]
        public CallTypeDto CallType { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("employee_resp_id")]
        public int? EmployeeRespId { get; set; }

        [JsonProperty("order_id")]
        public int? OrderId { get; set; }

        [JsonProperty("service_request_id")]
        public int? ServiceRequestId { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("contractor_id")]
        public int? ContractorId { get; set; }

        [JsonProperty("contractor")]
        public ContractorSimpleDto Contractor { get; set; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("call_from")]
        public DateTime CallFrom { get; set; }

        [JsonProperty("call_to")]
        public DateTime CallTo { get; set; }

        [JsonProperty("fio")]
        public string Fio { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("phone2")]
        public string Phone2 { get; set; }

        [JsonProperty("created_from")]
        public string CreatedFrom { get; set; }

        [JsonProperty("task")]
        public string Task { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("attempt")]
        public int Attempt { get; set; }

        [JsonProperty("employee_created_by_id")]
        public int EmployeeCreatedById { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("employee_completed_by_id")]
        public int? EmployeeCompletedById { get; set; }

        [JsonProperty("dependencies")]
        public List<CallDependencyDto> Dependencies { get; set; }

        [JsonProperty("completed_on")]
        public DateTime? CompletedOn { get; set; }
    }
}