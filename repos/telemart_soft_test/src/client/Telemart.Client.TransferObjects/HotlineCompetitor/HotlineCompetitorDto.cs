using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.HotlineCompetitor
{
    public class HotlineCompetitorDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("abc_id")]
        public int AbcId { get; set; }

        [JsonProperty("contractor_id")]
        public int? ContractorId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rating")]
        public double? Rating { get; set; }

        [JsonProperty("comment_count")]
        public int CommentCount { get; set; }
    }
}