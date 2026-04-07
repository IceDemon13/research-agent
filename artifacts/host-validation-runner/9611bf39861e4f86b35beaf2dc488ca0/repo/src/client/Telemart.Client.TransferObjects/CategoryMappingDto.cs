using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryMappingDto
    {
        [JsonProperty("item1")]
        public string Item1 { get; set; }

        [JsonProperty("item2")]
        public string Item2 { get; set; }
    }
}