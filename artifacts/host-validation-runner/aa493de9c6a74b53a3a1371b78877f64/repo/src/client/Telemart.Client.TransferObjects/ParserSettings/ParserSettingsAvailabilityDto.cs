using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSettings
{
    public class ParserSettingsAvailabilityDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parser_id")]
        public int ParserId { get; set; }

        [JsonProperty("availability_type_id")]
        public int? AvailabilityTypeId { get; set; }

        [JsonProperty("quantity")]
        public int? Quantity { get; set; }

        [JsonProperty("avail")]
        public string Avail { get; set; }
    }
}