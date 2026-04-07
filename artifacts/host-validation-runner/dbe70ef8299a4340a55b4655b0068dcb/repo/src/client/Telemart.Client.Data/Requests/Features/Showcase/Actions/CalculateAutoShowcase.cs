using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Paging;
using Telemart.Client.TransferObjects.Showcase;

namespace Telemart.Client.Data.Requests.Features.Showcase.Actions
{
    public sealed class CalculateAutoShowcase : CallActionWithBodyRequestResultBase<PagedResult<AutoShowcaseDto>, CalculateAutoShowcase.CalculateAutoShowcaseDto>
    {
        public CalculateAutoShowcase(int[] categoryIds, int[] warehouseIds, double segmentMinPercent)
            : base(new CalculateAutoShowcaseDto(categoryIds, warehouseIds, segmentMinPercent), ApiResources.Showcases, "auto")
        {
        }

        public sealed record CalculateAutoShowcaseDto
        {
            public CalculateAutoShowcaseDto(int[] categoryIds, int[] warehouseIds, double segmentMinPercent)
            {
                CategoryIds = categoryIds;
                WarehouseIds = warehouseIds;
                SegmentMinPercent = segmentMinPercent;
            }

            [JsonProperty("category_ids")]
            public int[] CategoryIds { get; }

            [JsonProperty("warehouse_ids")]
            public int[] WarehouseIds { get; }

            [JsonProperty("segment_min_percent")]
            public double SegmentMinPercent { get; }
        }
    }
}