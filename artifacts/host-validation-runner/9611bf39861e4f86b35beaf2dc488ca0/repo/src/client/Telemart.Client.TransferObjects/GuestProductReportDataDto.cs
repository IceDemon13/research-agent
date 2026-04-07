using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record GuestProductReportDataDto
    {
        [JsonProperty("product")]
        public string Product { get; init; }

        [JsonProperty("count")]
        public int Count { get; init; }

        [JsonProperty("sn")]
        public string Sn { get; init; }

        [JsonProperty("condition")]
        public string Condition { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }
    }
}