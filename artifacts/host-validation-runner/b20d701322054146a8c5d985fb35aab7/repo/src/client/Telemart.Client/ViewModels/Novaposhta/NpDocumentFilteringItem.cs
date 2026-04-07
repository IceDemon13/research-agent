using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Store
{
    public sealed class NpDocumentFilteringItem : PagingFilteringItem
    {
        [FilteringItemProperty("ids")]
        public string Ids { get; set; }

        [FilteringItemProperty("status_codes")]
        public string StatusCodes { get; set; }

        [FilteringItemProperty("entity_type_id")]
        public int? EntityTypeId { get; set; }

        [FilteringItemProperty("warehouse_id")]
        public int? WarehouseId { get; set; }

        [FilteringItemProperty("created_after")]
        public DateTime? CreatedAfter { get; set; }

        [FilteringItemProperty("created_before")]
        public DateTime? CreatedBefore { get; set; }

        [FilteringItemProperty("in_scan_sheet")]
        public bool? ScanSheet { get; set; }
    }
}