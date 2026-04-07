using System;
using System.Collections.Generic;
using System.Linq;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Service.ServiceProducts
{
    public sealed class ServiceProductFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("created_on_before")]
        public DateTime? CreatedOnBefore { get; set; }

        [FilteringItemProperty("created_on_after")]
        public DateTime? CreatedOnAfter { get; set; }

        [FilteringItemProperty("completed_on_before")]
        public DateTime? CompletedOnBefore { get; set; }

        [FilteringItemProperty("completed_on_after")]
        public DateTime? CompletedOnAfter { get; set; }

        [FilteringItemProperty("ids")]
        public string ServiceProductNumbers { get; set; }

        [FilteringItemProperty("service_request_ids")]
        public string ServiceRequestIds { get; set; }

        [FilteringItemProperty("product")]
        public string Product { get; set; }

        [FilteringItemProperty("sn")]
        public string SerialNumber { get; set; }

        [FilteringItemProperty("warehouse_ids")]
        public List<int> WarehouseIds { get; set; }

        [FilteringItemProperty("states")]
        public List<int> States { get; set; }
    }
}