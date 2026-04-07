using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Promo
{
    public class CatalogPromoSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("title_ukr")]
        public string TitleUkr { get; set; }

        [JsonProperty("title_en")]
        public string TitleEn { get; set; }

        [JsonProperty("date_start")]
        public DateTime DateStart { get; set; }

        [JsonProperty("date_end")]
        public DateTime DateEnd { get; set; }

        [JsonProperty("tag_description")]
        public string TagDescription { get; set; }

        [JsonProperty("tag_description_ukr")]
        public string TagDescriptionUkr { get; set; }

        [JsonProperty("tag_description_en")]
        public string TagDescriptionEn { get; set; }
    }
}