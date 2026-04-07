using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Segment
{
    public sealed class RecalculateSegmentsResultDto
    {
        [JsonProperty("parent_categories")]
        public List<RecalculateSegmentsResultCategoryDto> ParentCategories { get; set; }
    }
}