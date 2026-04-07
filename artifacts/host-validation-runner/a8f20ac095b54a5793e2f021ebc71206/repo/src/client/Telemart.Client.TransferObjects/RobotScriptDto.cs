using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class RobotScriptDto
    {
        [JsonProperty("catergory_id")]
        public int CategoryId { get; init; }

        [JsonProperty("parameters")]
        public string Parameters { get; init; }

        [JsonProperty("script")]
        public string Script { get; init; }
    }
}