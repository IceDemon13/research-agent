using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Order.Actions
{
    public sealed class UpdateOrderPrice : CallEntityActionWithBodyRequestResultBase<OrderDto, UpdateOrderPrice.OrderPriceSaveDto>
    {
        public UpdateOrderPrice(int orderId, bool freeDelivery, string comment, OrderProductPriceSaveDto[] productPrices)
            : base(orderId, new OrderPriceSaveDto(orderId, freeDelivery, comment, productPrices), ApiResources.Orders, "set_prices")
        {
        }

        public class OrderPriceSaveDto
        {
            public OrderPriceSaveDto(int id, bool freeDelivery, string comment, OrderProductPriceSaveDto[] productPrices)
            {
                Id = id;
                FreeDelivery = freeDelivery;
                Comment = comment;
                ProductPrices = productPrices;
            }

            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("free_delivery")]
            public bool FreeDelivery { get; set; }

            [JsonProperty("comment")]
            public string Comment { get; set; }

            [JsonProperty("product_prices")]
            public OrderProductPriceSaveDto[] ProductPrices { get; set; }
        }

        public class OrderProductPriceSaveDto
        {
            public OrderProductPriceSaveDto(int orderProductId, int productId, decimal price)
            {
                OrderProductId = orderProductId;
                ProductId = productId;
                Price = price;
            }

            [JsonProperty("order_product_id")]
            public int OrderProductId { get; set; }

            [JsonProperty("product_id")]
            public int ProductId { get; set; }

            [JsonProperty("price")]
            public decimal Price { get; set; }
        }
    }
}
