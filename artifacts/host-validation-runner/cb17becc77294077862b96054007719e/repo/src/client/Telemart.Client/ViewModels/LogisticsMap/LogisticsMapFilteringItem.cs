using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.LogisticsMap
{
    public class LogisticsMapFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("warehouse_ids")]
        public int[] WarehouseIds { get; init; }

        [FilteringItemProperty("supplier_ids")]
        public int[] SupplierIds { get; init; }
    }
}