using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class UsageReasonDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("feature_name")]
        public string FeatureName { get; set; }

        [JsonProperty("entity_id")]
        public int? EntityId { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }
    }
}