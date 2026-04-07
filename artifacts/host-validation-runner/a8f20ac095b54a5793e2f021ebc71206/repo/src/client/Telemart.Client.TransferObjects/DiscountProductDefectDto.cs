using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record DiscountProductDefectDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("defect")]
        public string Defect { get; init; }

        [JsonProperty("defect_ukr")]
        public string DefectUkr { get; init; }

        [JsonProperty("defect_en")]
        public string DefectEn { get; init; }

        [JsonProperty("description_template")]
        public string DescriptionTemplate { get; init; }

        [JsonProperty("description_template_ukr")]
        public string DescriptionTemplateUkr { get; init; }

        [JsonProperty("description_template_en")]
        public string DescriptionTemplateEn { get; init; }

        [JsonProperty("trade_in")]
        public bool TradeIn { get; init; }
    }
}