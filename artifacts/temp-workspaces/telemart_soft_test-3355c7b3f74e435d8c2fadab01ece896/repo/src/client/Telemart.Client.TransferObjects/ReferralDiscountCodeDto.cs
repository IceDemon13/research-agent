using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ReferralDiscountCodeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }
    }
}