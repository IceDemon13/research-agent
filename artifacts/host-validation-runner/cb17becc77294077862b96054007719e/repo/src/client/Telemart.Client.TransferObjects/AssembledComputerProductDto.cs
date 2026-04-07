using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record AssembledComputerProductDto
    {
        [JsonProperty("assembled_computer_id")]
        public int AssembledComputerId { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("scanned_quantity")]
        public int ScannedQuantity { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("price")]
        public decimal Price { get; init; }

        [JsonProperty("category_id")]
        public int CategoryId { get; init; }

        [JsonProperty("product_name")]
        public string ProductName { get; init; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; init; }

        [JsonProperty("serial_numbers")]
        public IReadOnlyCollection<AssembledComputerProductSnDto> SerialNumbers { get; init; }
    }
}