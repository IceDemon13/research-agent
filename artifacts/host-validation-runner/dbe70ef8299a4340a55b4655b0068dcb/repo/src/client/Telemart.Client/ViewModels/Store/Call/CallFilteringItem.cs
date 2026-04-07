using System;
using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Store.Call
{
    public sealed class CallFilteringItem : FilteringItemBase
    {
        private const string Separator = ",";

        [FilteringItemProperty("ids")]
        public string CallIds { get; set; }

        [FilteringItemProperty("order_ids")]
        public string OrderIds { get; set; }

        [FilteringItemProperty("service_request_ids")]
        public string ServiceRequestIds { get; set; }

        [FilteringItemProperty("call_created_from")]
        public DateTime? CallCreatedFrom { get; set; }

        [FilteringItemProperty("call_created_to")]
        public DateTime? CallCreatedTo { get; set; }

        [FilteringItemProperty("call_completed_from")]
        public DateTime? CallCompletedFrom { get; set; }

        [FilteringItemProperty("call_completed_to")]
        public DateTime? CallCompletedTo { get; set; }

        [FilteringItemProperty("subdivisions")]
        public List<int> Subdivisions { get; set; }

        [FilteringItemProperty("contractor")]
        public int? ContractorId { get; set; }

        [FilteringItemProperty("types")]
        public List<int> CallTypes { get; set; }

        [FilteringItemProperty("states")]
        public List<int> CallStates { get; set; }

        [FilteringItemProperty("fio")]
        public string Fio { get; set; }

        [FilteringItemProperty("phones")]
        public string PhoneNumbers { get; set; }

        [FilteringItemProperty("responsible_employees")]
        public List<int> ResponsibleEmployees { get; set; }

        [FilteringItemProperty("contractor_managers")]
        public List<int> ContractorManagers { get; set; }

        [FilteringItemProperty("completed_by_employees")]
        public List<int> CompletedByEmployees { get; set; }

        [FilteringItemProperty("available")]
        public bool? Available { get; set; }

        [FilteringItemProperty("completed")]
        public bool? Completed { get; set; }

        [FilteringItemProperty("order_id")]
        public int? OrderId { get; set; }
    }
}