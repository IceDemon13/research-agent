using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record CalculateOrderProductWarrantyEndDto
    {
        public CalculateOrderProductWarrantyEndDto(DateTime start, string serialNumber, int orderId, int productId)
        {
            Start = start;
            SerialNumber = serialNumber;
            OrderId = orderId;
            ProductId = productId;
        }

        [JsonProperty("start")]
        public DateTime Start { get; }

        [JsonProperty("serial_number")]
        public string SerialNumber { get; }

        [JsonProperty("order_id")]
        public int OrderId { get; }

        [JsonProperty("product_id")]
        public int ProductId { get; }
    }
}