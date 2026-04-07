using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class CategoryCreateDto
    {
        public CategoryCreateDto(
            string name,
            string nameUkr,
            string nameEn,
            string linkRewrite,
            CategoryCreateType type)
        {
            Name = name;
            NameUkr = nameUkr;
            NameEn = nameEn;
            LinkRewrite = linkRewrite;
            Type = type;
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("link_rewrite")]
        public string LinkRewrite { get; set; }

        [JsonProperty("type")]
        public CategoryCreateType Type { get; set; }
    }
}