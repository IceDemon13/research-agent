using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ParserSettings
{
    public sealed record ParserSettingsFilePriceDto
    {
        [JsonProperty("column_number")]
        public byte ColumnNumber { get; init; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; init; }

        [JsonProperty("parser_price_type_id")]
        public int ParserPriceTypeId { get; init; }
    }
}