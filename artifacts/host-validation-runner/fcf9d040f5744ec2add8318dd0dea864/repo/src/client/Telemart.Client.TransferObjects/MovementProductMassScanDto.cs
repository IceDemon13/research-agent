using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record MovementProductMassScanDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("quantity_out")]
        public int QuantityOut { get; init; }

        [JsonProperty("quantity_in")]
        public int QuantityIn { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; init; }

        [JsonProperty("serial_numbers")]
        public IReadOnlyCollection<MovementProductSnDto> SerialNumbers { get; init; }
    }
}