using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.LogisticsAnalitics
{
    public sealed class LogisticsAnaliticsFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("warehouse_ids")]
        public int[] WarehouseIds { get; set; }

        [FilteringItemProperty("expired_only")]
        public bool ExpiredOnly { get; set; }
    }
}