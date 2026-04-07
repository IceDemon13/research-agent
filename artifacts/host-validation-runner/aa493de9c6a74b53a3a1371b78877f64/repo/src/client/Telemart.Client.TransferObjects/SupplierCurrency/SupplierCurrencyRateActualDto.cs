using System;
using Newtonsoft.Json;
using Telemart.Common.Dictionaries;
using Telemart.Common.PriceConversion;

namespace Telemart.Client.TransferObjects.SupplierCurrency
{
    public sealed class SupplierCurrencyRateActualDto : IConversionRateCreator
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("supplier_id")]
        public int SupplierId { get; init; }

        [JsonProperty("currency_id")]
        public int CurrencyId { get; init; }

        [JsonProperty("rate")]
        public decimal Rate { get; init; }

        [JsonProperty("devotion")]
        public decimal Deviation { get; init; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; init; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; init; }

        [JsonProperty("modified_on")]
        public DateTime? ModifiedOn { get; init; }

        [JsonProperty("modified_by")]
        public int? ModifiedBy { get; init; }

        public ConversionRate CreateConversionRate()
        {
            return new ConversionRate(CurrencyId, Currency.UahId, Rate, Currency.GetById(CurrencyId).Decimals);
        }
    }
}