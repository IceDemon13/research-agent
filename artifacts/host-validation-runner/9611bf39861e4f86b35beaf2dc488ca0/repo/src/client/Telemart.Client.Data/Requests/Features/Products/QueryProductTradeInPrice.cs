using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Products
{
    public sealed class QueryProductTradeInPrice : CallActionWithBodyRequestResultBase<TradeInPriceCalculatorResultDto, QueryProductTradeInPrice.TradeInPriceCalculatorDto>
    {
        public QueryProductTradeInPrice(int orderId, int productId, string serialNumber = null)
            : base(new TradeInPriceCalculatorDto(orderId, productId, serialNumber), ApiResources.Products, "calculate_tradein_price")
        {
        }

        public class TradeInPriceCalculatorDto
        {
            public TradeInPriceCalculatorDto(int? orderId, int productId, string serialNumber)
            {
                OrderId = orderId;
                ProductId = productId;
                SerialNumber = serialNumber;
            }

            [JsonProperty("order_id")]
            public int? OrderId { get; set; }

            [JsonProperty("product_id")]
            public int ProductId { get; set; }

            [JsonProperty("serial_number")]
            public string SerialNumber { get; set; }
        }
    }
}