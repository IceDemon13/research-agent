using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCreatePackedSerialNumberDto
    {
        public OrderCreatePackedSerialNumberDto(int productId, string[] serialNumbers)
        {
            ProductId = productId;
            SerialNumbers = serialNumbers;
        }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("serial_numbers")]
        public string[] SerialNumbers { get; set; }
    }
}