using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class InvoiceProductComparisonDto
    {
        public InvoiceProductComparisonDto(
            int productId,
            int? quantityReal,
            IEnumerable<string> serialNumbers,
            IEnumerable<string> barcodes)
        {
            ProductId = productId;
            QuantityReal = quantityReal;
            SerialNumbers = serialNumbers?.ToList();
            Barcodes = barcodes?.ToList();
        }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("quantity_real")]
        public int? QuantityReal { get; set; }

        [JsonProperty("serial_numbers")]
        public List<string> SerialNumbers { get; set; }

        [JsonProperty("barcodes")]
        public List<string> Barcodes { get; set; }
    }
}