using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public sealed class RecalculateTradeInSegmentsDto
    {
        public RecalculateTradeInSegmentsDto(int[] parentCategoryIds)
        {
            ParentCategoryIds = parentCategoryIds;
        }

        [JsonProperty("parent_category_ids")]
        public int[] ParentCategoryIds { get; init; }
    }
}
