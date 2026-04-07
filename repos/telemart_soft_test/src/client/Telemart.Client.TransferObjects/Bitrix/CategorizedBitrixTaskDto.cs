using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Bitrix
{
    public class CategorizedBitrixTaskDto : BitrixTaskDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("our_priority_id")]
        public int OurPriorityId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("category_ids")]
        public int[] CategoryIds { get; set; }
    }
}