using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Backlog
{
    public class BacklogTaskCreateDto
    {
        [JsonProperty("author_id")]
        public int AuthorId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("bitrix_id")]
        public int? BitrixId { get; set; }

        [JsonProperty("formulation")]
        public string Formulation { get; set; }

        [JsonProperty("solution")]
        public string Solution { get; set; }

        [JsonProperty("justification")]
        public string Justification { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("category_ids")]
        public int[] CategoryIds { get; set; }
    }
}