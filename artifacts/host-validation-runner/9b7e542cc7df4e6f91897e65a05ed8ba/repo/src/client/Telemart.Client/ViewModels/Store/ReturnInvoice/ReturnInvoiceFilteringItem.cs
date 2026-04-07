using System;
using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Store.ReturnInvoice
{
    public sealed class ReturnInvoiceFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("contractor_ids")]
        public List<int> ContractorIds { get; set; }

        [FilteringItemProperty("warehouse_ids")]
        public List<int> WarehouseIds { get; set; }

        [FilteringItemProperty("state_ids")]
        public List<int> ReturnInvoiceStatesIds { get; set; }

        [FilteringItemProperty("return_ids")]
        public string ReturnInvoicesIds { get; set; }

        [FilteringItemProperty("invoice_ids")]
        public string InvoiceIds { get; set; }

        [FilteringItemProperty("ready_pack")]
        public bool? ReadyPack { get; set; }

        [FilteringItemProperty("returned_before")]
        public DateTime? ReturnedBefore { get; set; }

        [FilteringItemProperty("returned_after")]
        public DateTime? ReturnedAfter { get; set; }

        [FilteringItemProperty("created_before")]
        public DateTime? CreatedBefore { get; set; }

        [FilteringItemProperty("created_after")]
        public DateTime? CreatedAfter { get; set; }

        [FilteringItemProperty("product_id")]
        public int? ProductId { get; set; }

        [FilteringItemProperty("product")]
        public string ProductName { get; set; }
    }
}