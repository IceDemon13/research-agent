using System;

namespace Telemart.Client.ViewModels.Showcase
{
    public sealed class ShowcaseCategoryHistoryParameter
    {
        public ShowcaseCategoryHistoryParameter(DateTime? from, DateTime? to, int[] warehouseIds = null, int? categoryId = null)
        {
            From = from;
            To = to;
            WarehouseIds = warehouseIds;
            CategoryId = categoryId;
        }

        public DateTime? From { get; }

        public DateTime? To { get; }

        public int[] WarehouseIds { get; }

        public int? CategoryId { get; }
    }
}