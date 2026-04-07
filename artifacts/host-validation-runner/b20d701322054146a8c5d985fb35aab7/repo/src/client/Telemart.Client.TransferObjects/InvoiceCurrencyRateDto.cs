using System;
using Newtonsoft.Json;
using Telemart.Common.Dictionaries;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.TransferObjects
{
    public sealed record InvoiceCurrencyRateDto : IConversionRateCreator
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("from_currency_id")]
        public int FromCurrencyId { get; set; }

        [JsonProperty("conversion_rate")]
        public decimal ConversionRate { get; set; }

        [JsonProperty("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        public ConversionRate CreateConversionRate()
        {
            return new ConversionRate(
                FromCurrencyId,
                Currency.UahId,
                ConversionRate,
                Currency.GetById(FromCurrencyId).Decimals);
        }
    }
}