using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Bundle
{
    public record BundleDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("promo_code")]
        public string PromoCode { get; init; }

        [JsonProperty("date_start")]
        public DateTime DateStart { get; init; }

        [JsonProperty("date_end")]
        public DateTime DateEnd { get; init; }

        [JsonProperty("products")]
        public IReadOnlyCollection<BundleProductDto> Products { get; init; }

        [JsonProperty("categories")]
        public IReadOnlyCollection<BundleCategoryDto> Categories { get; init; }

        [JsonProperty("examples")]
        public IReadOnlyCollection<ProductDto[]> Examples { get; init; }
    }
}
