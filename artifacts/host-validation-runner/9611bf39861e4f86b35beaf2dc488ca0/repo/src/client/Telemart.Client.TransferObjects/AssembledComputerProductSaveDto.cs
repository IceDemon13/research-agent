using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AssembledComputerProductSaveDto
    {
        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("scanned_quantity")]
        public int ScannedQuantity { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("price")]
        public decimal Price { get; init; }

        [JsonProperty("serial_numbers")]
        public IReadOnlyCollection<string> SerialNumbers { get; init; }
    }
}