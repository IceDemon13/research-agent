using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Service.ServiceRequests
{
    public class ServiceRequestFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("cities")]
        public int[] Cities { get; set; }

        [FilteringItemProperty("contractors")]
        public int[] Contractors { get; set; }

        [FilteringItemProperty("after")]
        public DateTime? CreatedAfter { get; set; }

        [FilteringItemProperty("before")]
        public DateTime? CreatedBefore { get; set; }

        [FilteringItemProperty("changed_after")]
        public DateTime? ChangedAfter { get; set; }

        [FilteringItemProperty("changed_before")]
        public DateTime? ChangedBefore { get; set; }

        [FilteringItemProperty("fio")]
        public string Fio { get; set; }

        [FilteringItemProperty("managers")]
        public int[] Managers { get; set; }

        [FilteringItemProperty("group_id")]
        public int? GroupId { get; set; }

        [FilteringItemProperty("warehouse_location_id")]
        public int? WarehouseLocationId { get; set; }

        [FilteringItemProperty("location")]
        public int? Location { get; set; }

        [FilteringItemProperty("orders")]
        public string OrderNumbers { get; set; }

        [FilteringItemProperty("phone")]
        public string Phone { get; set; }

        [FilteringItemProperty("email")]
        public string Email { get; set; }

        [FilteringItemProperty("include_customer_state_text")]
        public bool IncludeCustomerStateText { get; set; }

        [FilteringItemProperty("product")]
        public string Product { get; set; }

        [FilteringItemProperty("ids")]
        public string RequestNumbers { get; set; }

        [FilteringItemProperty("sn")]
        public string SerialNumbers { get; set; }

        [FilteringItemProperty("bundle")]
        public bool? Bundle { get; set; }

        [FilteringItemProperty("product_ids")]
        public int[] ProductIds { get; set; }

        [FilteringItemProperty("states")]
        public int[] States { get; set; }

        [FilteringItemProperty("dstates")]
        public int[] DiscussionStates { get; set; }

        [FilteringItemProperty("subdivisions")]
        public int[] Subdivisions { get; set; }

        [FilteringItemProperty("requirements")]
        public int[] RequirementTypes { get; set; }

        [FilteringItemProperty("reject_reasons")]
        public int[] RejectReasons { get; set; }

        [FilteringItemProperty("resolutions")]
        public int[] ResolutionTypes { get; set; }

        [FilteringItemProperty("ttn")]
        public string TrackNumber { get; set; }

        [FilteringItemProperty("completed_on_money_refunds")]
        public bool? CompletedOnMoneyRefund { get; set; }

        [FilteringItemProperty("completed_on_fiscal_registrar")]
        public bool? CompletedOnFiscalRegistrar { get; set; }
    }
}