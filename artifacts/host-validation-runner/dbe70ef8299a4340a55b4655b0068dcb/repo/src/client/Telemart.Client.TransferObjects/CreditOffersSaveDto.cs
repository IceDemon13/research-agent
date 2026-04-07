using System.Collections.Generic;
using Newtonsoft.Json;
using Telemart.Common.TransferObjects;

namespace Telemart.Client.TransferObjects
{
    public record CreditOffersSaveDto
    {
        [JsonProperty("min_product_margin_percent")]
        public decimal MinProductMarginPercent { get; init; }

        [JsonProperty("min_partial_pay_count")]
        public int? MinPartialPayCount { get; init; }

        [JsonProperty("credit_offers")]
        public IReadOnlyCollection<CreditOfferDto> CreditOffers { get; init; }
    }
}