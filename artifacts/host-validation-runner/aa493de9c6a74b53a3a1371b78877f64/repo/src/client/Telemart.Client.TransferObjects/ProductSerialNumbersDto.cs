using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductSerialNumbersDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("serial_numbers")]
        public string[] SerialNumbers { get; set; }
    }
}