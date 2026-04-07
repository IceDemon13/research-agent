using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeInSegment
{
    public sealed class RecalculateTradeInSegmentsResultDto
    {
        [JsonProperty("parent_categories")]
        public List<RecalculateTradeInSegmentsResultCategoryDto> ParentCategories { get; init; }
    }
}
