using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class InvoiceFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("contractor_ids")]
        public int[] ContractorIds { get; set; }

        [FilteringItemProperty("date_after")]
        public DateTime? InvoiceGetAfter { get; set; }

        [FilteringItemProperty("date_before")]
        public DateTime? InvoiceGetBefore { get; set; }

        [FilteringItemProperty("ids")]
        public string InvoicesIds { get; set; }

        [FilteringItemProperty("state_ids")]
        public int[] InvoiceStatesIds { get; set; }

        [FilteringItemProperty("warehouses_ids")]
        public int[] WarehousesIds { get; set; }

        [FilteringItemProperty("product_id")]
        public int? ProductId { get; set; }
    }
}