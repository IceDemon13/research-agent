using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record ContractorProductExtraChargeDto
    {
        [JsonProperty("contractor_id")]
        public int ContractorId { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("extra_charge")]
        public decimal ExtraCharge { get; init; }
    }
}