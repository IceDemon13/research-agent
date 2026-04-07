using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AssemblyTestSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("main_category_id")]
        public int MainCategoryId { get; set; }

        [JsonProperty("main_feature_ids")]
        public int[] MainFeatureIds { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("regex")]
        public string Regex { get; set; }

        [JsonProperty("suffix")]
        public string Suffix { get; set; }

        [JsonProperty("avail_on_web")]
        public bool AvailOnWeb { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }

        [JsonProperty("slave_categories")]
        public AssemblyTestSlaveCategorySaveDto[] SlaveCategories { get; set; }
    }
}