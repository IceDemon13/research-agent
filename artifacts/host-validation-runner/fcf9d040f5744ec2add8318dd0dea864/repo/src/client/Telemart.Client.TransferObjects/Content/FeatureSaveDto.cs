using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Content
{
    public class FeatureSaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("separator")]
        public string Separator { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("hint_ukr")]
        public string HintUkr { get; set; }

        [JsonProperty("hint_en")]
        public string HintEn { get; set; }

        [JsonProperty("suffix")]
        public string Suffix { get; set; }

        [JsonProperty("suffix_ukr")]
        public string SuffixUkr { get; set; }

        [JsonProperty("suffix_en")]
        public string SuffixEn { get; set; }

        [JsonProperty("mask")]
        public string Mask { get; set; }

        [JsonProperty("regex")]
        public string Regex { get; set; }

        [JsonProperty("show_on_site")]
        public bool ShowOnSite { get; set; }

        [JsonProperty("hide_minus")]
        public bool HideMinus { get; set; }

        [JsonProperty("multi_language")]
        public bool MultiLanguage { get; set; }

        [JsonProperty("manual_input")]
        public bool ManualInput { get; set; }

        [JsonProperty("print_in_tags")]
        public bool PrintInTags { get; set; }

        [JsonProperty("settings")]
        public bool Settings { get; set; }

        [JsonProperty("feature_option_id")]
        public int? FeatureOptionId { get; set; }

        [JsonProperty("feature_value_discount_id")]
        public int? FeatureValueDiscountId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("group_id")]
        public int GroupId { get; set; }

        [JsonProperty("change_image")]
        public bool? ChangeImage { get; set; }

        [JsonProperty("icon_bytes")]
        public byte[] IconBytes { get; set; }

        [JsonProperty("feature_contractor_keys")]
        public IReadOnlyCollection<FeatureContractorKeyDto> FeatureContractorKeys { get; set; }
    }
}