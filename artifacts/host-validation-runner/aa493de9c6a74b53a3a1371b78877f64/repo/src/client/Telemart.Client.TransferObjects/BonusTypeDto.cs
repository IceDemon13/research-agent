using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record BonusTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("payment_id")]
        public int? PaymentId { get; init; }

        [JsonProperty("cashbox_id")]
        public int CashboxId { get; init; }

        [JsonProperty("life_time")]
        public string LifeTime { get; init; }
    }
}