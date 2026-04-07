using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record CurrencyTypeRateDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("from_currency_type_id")]
        public int FromCurrencyTypeId { get; set; }

        [JsonProperty("to_currency_type_id")]
        public int ToCurrencyTypeId { get; set; }

        [JsonProperty("from_currency_id")]
        public int FromCurrencyId { get; set; }

        [JsonProperty("to_currency_id")]
        public int ToCurrencyId { get; set; }

        [JsonProperty("conversion_rate")]
        public decimal ConversionRate { get; set; }

        [JsonProperty("digits")]
        public int Digits { get; set; }
    }
}