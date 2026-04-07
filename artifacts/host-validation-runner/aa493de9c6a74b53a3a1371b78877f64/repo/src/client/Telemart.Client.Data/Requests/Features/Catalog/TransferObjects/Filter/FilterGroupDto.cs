using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.Catalog.TransferObjects.Filter
{
    public class FilterGroupDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("expanded")]
        public bool Expanded { get; set; }

        [JsonProperty("filters")]
        public IReadOnlyCollection<FilterDto> Filters { get; set; }
    }
}