using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Comment
{
    public class CommentCreateAnswerDto
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("customer_id")]
        public int? CustomerId { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }
    }
}