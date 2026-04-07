using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public sealed class FeatureGroupSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }
}