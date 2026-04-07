using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Debezium
{
    public sealed record ShowcaseCategoryHistoriesDto
    {
        [JsonProperty("showcase_category_histories")]
        public IReadOnlyCollection<ShowcaseCategoryHistoryDto> ShowcaseCategoryHistories { get; init; }
    }
}