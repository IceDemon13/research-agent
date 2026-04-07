using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class TagFormatDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }
}