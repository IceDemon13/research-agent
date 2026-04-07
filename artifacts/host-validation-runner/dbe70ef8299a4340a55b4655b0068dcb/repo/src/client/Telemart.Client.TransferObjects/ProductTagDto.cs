using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductTagDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("link_ukr")]
        public string LinkUkr { get; set; }

        [JsonProperty("link_en")]
        public string LinkEn { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("category_ukr")]
        public string CategoryUkr { get; set; }

        [JsonProperty("category_en")]
        public string CategoryEn { get; set; }

        [JsonProperty("bonus_amount")]
        public int? BonusAmount { get; set; }

        [JsonProperty("color_id")]
        public int ColorId { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("price_prev")]
        public int PricePrev { get; set; }

        [JsonProperty("tag_format_id")]
        public int TagFormatId { get; set; }

        [JsonProperty("active_promo")]
        public bool ActivePromo { get; set; }

        [JsonProperty("print_reason_data")]
        public IReadOnlyCollection<string> PrintReasonData { get; set; }
    }
}