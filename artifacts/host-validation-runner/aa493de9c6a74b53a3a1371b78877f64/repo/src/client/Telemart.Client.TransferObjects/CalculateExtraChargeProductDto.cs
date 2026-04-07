using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record CalculateExtraChargeProductDto
    {
        public CalculateExtraChargeProductDto(int productId, decimal priceIn, decimal priceOut, int contractorId)
        {
            ProductId = productId;
            PriceIn = priceIn;
            PriceOut = priceOut;
            ContractorId = contractorId;
        }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("price_in")]
        public decimal PriceIn { get; init; }

        [JsonProperty("price_out")]
        public decimal PriceOut { get; init; }

        [JsonProperty("contractor_id")]
        public int ContractorId { get; init; }
    }
}