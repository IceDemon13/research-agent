using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderProductDeliveryDto
    {
        public OrderProductDeliveryDto(int orderProductId, IReadOnlyCollection<string> serials)
        {
            OrderProductId = orderProductId;
            Serials = serials;
        }

        [JsonProperty("order_product_id")]
        public int OrderProductId { get; set; }

        [JsonProperty("serials")]
        public IReadOnlyCollection<string> Serials { get; set; }
    }
}