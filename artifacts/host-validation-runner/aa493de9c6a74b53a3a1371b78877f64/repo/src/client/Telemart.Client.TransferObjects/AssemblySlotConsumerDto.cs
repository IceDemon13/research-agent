using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssemblySlotConsumerDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("slot_host_id")]
        public int SlotHostId { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("feature_id")]
        public int? FeatureId { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}