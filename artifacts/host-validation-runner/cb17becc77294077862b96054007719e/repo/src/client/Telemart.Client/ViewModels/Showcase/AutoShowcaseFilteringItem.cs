using System.Diagnostics.CodeAnalysis;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Showcase
{
    public class AutoShowcaseFilteringItem : FilteringItemBase
    {
        public AutoShowcaseFilteringItem([NotNull]int[] warehouseIds,  int[] clusterIds, int? categoryEmployeeId, int? categoryId)
        {
            WarehouseIds = warehouseIds;
            CategoryEmployeeId = categoryEmployeeId;
            CategoryId = categoryId;
            ClusterIds = clusterIds;
        }

        [FilteringItemProperty("warehouse_ids")]
        public int[] WarehouseIds { get; }

        [FilteringItemProperty("cluster_ids")]
        public int[] ClusterIds { get; }

        [FilteringItemProperty("category_employee_id")]
        public int? CategoryEmployeeId { get; }

        [FilteringItemProperty("category_id")]
        public int? CategoryId { get; }
    }
}