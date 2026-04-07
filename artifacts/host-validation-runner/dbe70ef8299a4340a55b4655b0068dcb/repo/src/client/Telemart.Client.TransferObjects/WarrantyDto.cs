using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class WarrantyDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ua")]
        public string NameUa { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("name_short")]
        public string ShortName { get; set; }

        [JsonProperty("name_short_ua")]
        public string ShortNameUa { get; set; }

        [JsonProperty("name_short_en")]
        public string ShortNameEn { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("trade_in_default")]
        public bool TradeInDefault { get; set; }
    }
}