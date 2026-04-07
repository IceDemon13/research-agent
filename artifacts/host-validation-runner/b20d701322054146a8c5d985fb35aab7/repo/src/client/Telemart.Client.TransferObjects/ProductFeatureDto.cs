using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductFeatureDto
    {
        [JsonProperty("fid")]
        public int Id { get; set; }

        [JsonProperty("fname")]
        public string Name { get; set; }

        [JsonProperty("fname_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("fname_en")]
        public string NameEn { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("value_id")]
        public int ValueId { get; set; }

        [JsonProperty("value_ukr")]
        public string ValueUkr { get; set; }

        [JsonProperty("value_en")]
        public string ValueEn { get; set; }

        [JsonProperty("suffix")]
        public string Suffix { get; set; }

        [JsonProperty("suffix_ukr")]
        public string SuffixUkr { get; set; }

        [JsonProperty("suffix_en")]
        public string SuffixEn { get; set; }

        [JsonProperty("hide")]
        public bool Hide { get; set; }

        [JsonProperty("print_in_tags")]
        public bool PrintInTags { get; set; }
    }
}