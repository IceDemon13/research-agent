using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class RecognizeSupplierProductDto
    {
        public RecognizeSupplierProductDto(
            int? productId,
            string productName,
            string supplierProductId,
            string supplierProductName,
            string pn)
        {
            ProductId = productId;
            SupplierProductId = supplierProductId;
            SupplierProductName = supplierProductName;
            Pn = pn;
            ProductName = productName;
        }

        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("supplier_product_id")]
        public string SupplierProductId { get; set; }

        [JsonProperty("supplier_product_name")]
        public string SupplierProductName { get; set; }

        [JsonProperty("pn")]
        public string Pn { get; set; }
    }
}