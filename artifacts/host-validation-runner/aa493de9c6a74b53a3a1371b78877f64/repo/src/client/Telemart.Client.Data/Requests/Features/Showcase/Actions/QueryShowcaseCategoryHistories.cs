using System;
using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Debezium;

namespace Telemart.Client.Data.Requests.Features.Showcase.Actions
{
    public sealed class QueryShowcaseCategoryHistories : CallActionWithBodyRequestResultBase<ShowcaseCategoryHistoriesDto,QueryShowcaseCategoryHistories.QueryShowcaseCategoryHistoriesRequest>
    {
        public QueryShowcaseCategoryHistories(DateTime? from, DateTime? to, int[] warehouseIds, int? categoryId)
            : base(new QueryShowcaseCategoryHistoriesRequest(from, to, warehouseIds, categoryId), ApiResources.Showcases, "showcase_category_history")
        {

        }

        public sealed class QueryShowcaseCategoryHistoriesRequest
        {
            public QueryShowcaseCategoryHistoriesRequest(DateTime? from, DateTime? to, int[] warehouseIds, int? categoryId)
            {
                From = from;
                To = to;
                WarehouseIds = warehouseIds;
                CategoryId = categoryId;
            }

            [JsonProperty("from")]
            public DateTime? From { get; init; }

            [JsonProperty("to")]
            public DateTime? To { get; init; }

            [JsonProperty("warehouse_ids")]
            public int[] WarehouseIds { get; set; }

            [JsonProperty("category_id")]
            public int? CategoryId { get; set; }
        }
    }
}