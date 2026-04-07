using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderFolderDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("product_id")]
        public int? ProductId { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("type_id")]
        public int TypeId { get; init; }

        [JsonProperty("price")]
        public decimal? Price { get; set; }

        [JsonProperty("price_id")]
        public int? PriceId { get; init; }

        [JsonProperty("bonuses_to_charge")]
        public int? BonusesToCharge { get; init; }

        [JsonProperty("free_delivery")]
        public bool FreeDelivery { get; set; }
    }
}