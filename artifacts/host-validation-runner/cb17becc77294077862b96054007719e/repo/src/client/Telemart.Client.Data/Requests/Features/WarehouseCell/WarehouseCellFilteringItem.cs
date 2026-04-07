using Telemart.Client.Data.WebClient;

namespace Telemart.Client.Data.Requests.Features.WarehouseCell
{
    public class WarehouseCellFilteringItem : FilteringItemBase
    {
        public WarehouseCellFilteringItem(int warehouseId)
        {
            WarehouseId = warehouseId;
        }

        [FilteringItemProperty("warehouseId")]
        public int WarehouseId { get; }

        [FilteringItemProperty("ids")]
        public int[] Ids { get; set; }

        [FilteringItemProperty("name")]
        public string Name { get; set; }

        [FilteringItemProperty("used")]
        public bool? Used { get; set; }

        [FilteringItemProperty("active")]
        public bool? Active { get; set; }
    }
}