using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryPositionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }
}