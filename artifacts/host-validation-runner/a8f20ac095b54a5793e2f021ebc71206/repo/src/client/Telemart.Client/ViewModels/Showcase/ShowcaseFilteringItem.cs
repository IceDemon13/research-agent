using System;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Showcase
{
    public class ShowcaseFilteringItem : FilteringItemBase
    {
        public ShowcaseFilteringItem(int? productId, int[] warehouseIds, int? categoryEmployeeId, int? categoryId, DateTime? from, DateTime? to)
        {
            ProductId = productId;
            WarehouseIds = warehouseIds;
            CategoryEmployeeId = categoryEmployeeId;
            CategoryId = categoryId;
            From = from;
            To = to;
        }

        [FilteringItemProperty("product_id")]
        public int? ProductId { get; }

        [FilteringItemProperty("warehouse_ids")]
        public int[] WarehouseIds { get; }

        [FilteringItemProperty("category_employee_id")]
        public int? CategoryEmployeeId { get; }

        [FilteringItemProperty("category_id")]
        public int? CategoryId { get; }

        [FilteringItemProperty("from")]
        public DateTime? From { get; }

        [FilteringItemProperty("to")]
        public DateTime? To { get; }
    }
}