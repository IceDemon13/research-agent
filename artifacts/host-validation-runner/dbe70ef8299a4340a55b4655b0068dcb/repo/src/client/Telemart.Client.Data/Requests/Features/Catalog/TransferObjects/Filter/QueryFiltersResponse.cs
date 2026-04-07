using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.Filter
{
    public class QueryFiltersResponse
    {
        [JsonProperty("price_min_uah")]
        public decimal PriceMin { get; set; }

        [JsonProperty("price_max_uah")]
        public decimal PriceMax { get; set; }

        [JsonProperty("labels")]
        public IReadOnlyCollection<LabelDto> Labels { get; set; }

        [JsonProperty("filter_groups")]
        public IReadOnlyCollection<FilterGroupDto> FilterGroups { get; set; }
    }
}