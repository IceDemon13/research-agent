using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.AssemblyService
{
    public class AssemblyServicesFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("ids")]
        public string Ids { get; set; }

        [FilteringItemProperty("order_ids")]
        public string OrderIds { get; set; }

        [FilteringItemProperty("state_ids")]
        public string StateIds { get; set; }

        [FilteringItemProperty("assembly_date_before")]
        public DateTime? AssemblyDateBefore { get; set; }

        [FilteringItemProperty("assembly_date_after")]
        public DateTime? AssemblyDateAfter { get; set; }

        [FilteringItemProperty("order_delivery_time_to_before")]
        public DateTime? OrderDeliveryTimeToBefore { get; set; }

        [FilteringItemProperty("order_delivery_time_to_after")]
        public DateTime? OrderDeliveryTimeToAfter { get; set; }

        [FilteringItemProperty("assembly_service_product_id")]
        public int? AssemblyServiceProductId { get; set; }

        [FilteringItemProperty("product_id")]
        public int? ProductId { get; set; }

        [FilteringItemProperty("subdivision_id")]
        public int? SubdivisionId { get; set; }

        [FilteringItemProperty("order_state_ids")]
        public string OrderStateIds { get; set; }

        [FilteringItemProperty("parent_ids")]
        public string ParentIds { get; set; }
    }
}