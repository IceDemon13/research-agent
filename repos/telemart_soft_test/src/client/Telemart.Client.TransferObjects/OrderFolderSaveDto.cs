using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class OrderFolderSaveDto
    {
        public OrderFolderSaveDto(
            int id,
            int? productId,
            int quantity,
            string name,
            int typeId,
            decimal? price,
            int? bonusesToCharge,
            int? priceId,
            bool freeDelivery)
        {
            Id = id;
            ProductId = productId;
            Quantity = quantity;
            Name = name;
            TypeId = typeId;
            Price = price;
            PriceId = priceId;
            BonusesToCharge = bonusesToCharge;
            FreeDelivery = freeDelivery;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("price")]
        public decimal? Price { get; set; }

        [JsonProperty("price_id")]
        public int? PriceId { get; set; }

        [JsonProperty("bonuses_to_charge")]
        public int? BonusesToCharge { get; set; }

        [JsonProperty("free_delivery")]
        public bool FreeDelivery { get; set; }
    }
}