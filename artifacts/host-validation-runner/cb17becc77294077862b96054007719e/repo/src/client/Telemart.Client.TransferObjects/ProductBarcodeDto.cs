using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductBarcodeDto
    {
        [JsonProperty("barcode_id")]
        public int Id { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        [JsonProperty("fiscal_registrar")]
        public bool FiscalRegistrar { get; init; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }

        [JsonProperty("modified_by")]
        public int ModifiedBy { get; set; }
    }
}