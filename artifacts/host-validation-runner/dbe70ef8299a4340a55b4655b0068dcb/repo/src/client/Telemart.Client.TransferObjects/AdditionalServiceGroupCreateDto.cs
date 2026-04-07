using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AdditionalServiceGroupCreateDto
    {
        [JsonProperty("parent_id")]
        public int? ParentId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("description_ua")]
        public string DescriptionUa { get; set; }

        [JsonProperty("description_en")]
        public string DescriptionEn { get; set; }

        [JsonProperty("multi_select")]
        public bool MultiSelect { get; set; }
    }
}