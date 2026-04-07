using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderSaleDto
    {
        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("order_client_id")]
        public int OrderClientId { get; set; }

        [JsonProperty("order_created_on")]
        public DateTime OrderCreatedOn { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("serial_number")]
        public OrderProductSnDto SerialNumber { get; set; }
    }
}