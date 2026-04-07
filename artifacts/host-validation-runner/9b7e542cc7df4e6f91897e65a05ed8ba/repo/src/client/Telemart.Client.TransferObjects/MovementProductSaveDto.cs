using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class MovementProductSaveDto
    {
        public MovementProductSaveDto(
            int productId,
            int quantity,
            int quantityOut,
            int quantityIn,
            List<MovementProductSnDto> serialNumbers)
        {
            ProductId = productId;
            Quantity = quantity;
            QuantityOut = quantityOut;
            QuantityIn = quantityIn;
            SerialNumbers = serialNumbers;
        }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("quantity_out")]
        public int QuantityOut { get; set; }

        [JsonProperty("quantity_in")]
        public int QuantityIn { get; set; }

        [JsonProperty("serial_numbers")]
        public List<MovementProductSnDto> SerialNumbers { get; set; }
    }
}