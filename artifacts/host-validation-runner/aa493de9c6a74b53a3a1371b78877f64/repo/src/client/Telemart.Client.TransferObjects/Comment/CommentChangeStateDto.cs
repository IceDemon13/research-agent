using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Comment
{
    public class CommentChangeStateDto
    {
        [JsonProperty("state_id")]
        public int StateId { get; set; }
    }
}