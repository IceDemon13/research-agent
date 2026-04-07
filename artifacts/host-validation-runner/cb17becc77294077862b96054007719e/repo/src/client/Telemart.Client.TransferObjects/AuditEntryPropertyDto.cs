using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AuditEntryPropertyDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("property_name")]
        public string PropertyName { get; set; }

        [JsonProperty("relation_name")]
        public string RelationName { get; set; }

        [JsonProperty("new_value")]
        public string NewValueFormatted { get; set; }

        [JsonProperty("old_value")]
        public string OldValueFormatted { get; set; }
    }
}