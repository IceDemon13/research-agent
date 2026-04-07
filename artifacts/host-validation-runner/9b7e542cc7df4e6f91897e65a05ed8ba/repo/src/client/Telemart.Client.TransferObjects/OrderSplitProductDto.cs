using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderSplitProductDto
    {
        public OrderSplitProductDto(int orderId, int orderProductId, int splitQuantity)
        {
            Id = orderId;
            OrderProductId = orderProductId;
            SplitQuantity = splitQuantity;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("order_product_id")]
        public int OrderProductId { get; set; }

        [JsonProperty("split_quantity")]
        public int SplitQuantity { get; set; }
    }
}