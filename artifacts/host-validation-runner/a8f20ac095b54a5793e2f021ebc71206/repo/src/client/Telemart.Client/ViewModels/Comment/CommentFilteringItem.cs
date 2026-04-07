using System;
using System.Collections.Generic;
using Telemart.Client.Data.WebClient;

namespace Telemart.Client.ViewModels.Comment
{
    public class CommentFilteringItem : FilteringItemBase
    {
        [FilteringItemProperty("created_on_from")]
        public DateTime? CreatedOnFrom { get; init; }

        [FilteringItemProperty("date_from")]
        public DateTime? DateFrom { get; init; }

        [FilteringItemProperty("date_to")]
        public DateTime? DateTo { get; init; }

        [FilteringItemProperty("take")]
        public int? Take { get; init; }

        [FilteringItemProperty("include_children")]
        public bool? IncludeChildren { get; init; }

        [FilteringItemProperty("product_manager_ids")]
        public List<int> ProductManagerIds { get; init; }

        [FilteringItemProperty("category_id")]
        public int? CategoryId { get; init; }

        [FilteringItemProperty("created_by")]
        public int? CreatedBy { get; init; }

        [FilteringItemProperty("product")]
        public string Product { get; init; }

        [FilteringItemProperty("types")]
        public List<int> Types { get; init; }

        [FilteringItemProperty("states")]
        public List<int> Statuses { get; init; }

        [FilteringItemProperty("stars_avg_from")]
        public decimal? StarsAvgFrom { get; init; }

        [FilteringItemProperty("stars_avg_to")]
        public decimal? StarsAvgTo { get; init; }

        [FilteringItemProperty("product_ratio_from")]
        public int? ProductRatioFrom { get; init; }

        [FilteringItemProperty("product_ratio_to")]
        public int? ProductRatioTo { get; init; }
    }
}