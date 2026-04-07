using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderMergeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("source_ids")]
        public int[] SourceIds { get; set; }
    }
}