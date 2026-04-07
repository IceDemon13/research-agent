using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record CalculateExtraChargeDto
    {
        public CalculateExtraChargeDto(IReadOnlyCollection<CalculateExtraChargeProductDto> products)
        {
            Products = products;
        }

        [JsonProperty("products")]
        public IReadOnlyCollection<CalculateExtraChargeProductDto> Products { get; init; }
    }
}