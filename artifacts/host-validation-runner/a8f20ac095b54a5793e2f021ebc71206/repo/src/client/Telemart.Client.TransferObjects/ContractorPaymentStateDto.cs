using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ContractorPaymentStateDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("description")]
        public string Description { get; init; }

        [JsonProperty("description_ukr")]
        public string DescriptionUkr { get; init; }

        [JsonProperty("payment_id")]
        public int PaymentId { get; init; }

        [JsonProperty("payment_state_id")]
        public int PaymentStateId { get; init; }
    }
}