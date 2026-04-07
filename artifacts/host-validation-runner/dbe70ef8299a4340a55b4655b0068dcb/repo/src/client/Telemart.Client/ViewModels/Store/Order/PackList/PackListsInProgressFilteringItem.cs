using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Store.Order.PackList
{
    public sealed class PackListsInProgressFilteringItem : FilteringItemBase
    {
        public PackListsInProgressFilteringItem(int? warehouseId, bool? checkPackLists = null)
        {
            WarehouseId = warehouseId;
            CheckPackLists = checkPackLists;
        }

        [FilteringItemProperty("warehouse_id")]
        public int? WarehouseId { get; set; }

        [FilteringItemProperty("check_pack_lists")]
        public bool? CheckPackLists { get; set; }
    }
}