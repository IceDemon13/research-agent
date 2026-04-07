using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceEditingInfoDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("ignore_transit")]
        public bool IgnoreTransit { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("employee_lock_id")]
        public int? EmployeeLockId { get; set; }

        [JsonProperty("employee_lock")]
        public EmployeeSimpleDto EmployeeLock { get; set; }

        [JsonProperty("before_closing_time")]
        public TimeSpan? BeforeClosingTime { get; set; }

        [JsonProperty("editors")]
        public IReadOnlyCollection<string> Editors { get; set; }
    }
}