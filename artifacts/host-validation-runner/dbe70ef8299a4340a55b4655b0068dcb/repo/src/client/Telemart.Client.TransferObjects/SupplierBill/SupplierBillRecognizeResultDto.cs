using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.SupplierBill
{
    public class SupplierBillRecognizeResultDto : SupplierBillRecognizeDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("product_tax_rate_id")]
        public int ProductTaxRateId { get; set; }

        [JsonProperty("modified_on")]
        public DateTime ModifiedOn { get; set; }
    }
}