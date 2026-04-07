using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Novaposhta
{
    public class CourierCallFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("warehouse_id")]
        public int? WarehouseId { get; set; }

        [FilteringItemProperty("date_from")]
        public DateTime? From { get; set; }

        [FilteringItemProperty("date_to")]
        public DateTime? To { get; set; }

        [FilteringItemProperty("ttns")]
        public string? Ttns { get; set; }

        [FilteringItemProperty("barcode")]
        public string? Barcode { get; set; }

        [FilteringItemProperty("status")]
        public string? Statuses { get; set; }

        [FilteringItemProperty("completed")]
        public bool? Completed { get; set; }
    }
}
