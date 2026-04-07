using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public sealed class RecalculateSegmentsDto
    {
        public RecalculateSegmentsDto(int[] parentCategoryIds)
        {
            ParentCategoryIds = parentCategoryIds;
        }

        [JsonProperty("parent_category_ids")]
        public int[] ParentCategoryIds { get; set; }
    }
}