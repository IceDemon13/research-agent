using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductComparsionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pn")]
        public string PartNumber { get; set; }

        [JsonProperty("cat_id")]
        public int ParentCategoryId { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("manufactor")]
        public string Manufactor { get; set; }
    }
}