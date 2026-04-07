using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Debezium
{
    public sealed class ShowcaseHistoriesDto
    {
        [JsonProperty("category_id")]
        public int? CategoryId { get; set; }

        [JsonProperty("history")]
        public IReadOnlyCollection<ShowcaseHistoryDto> ShowcaseHistories { get; set; }
    }
}