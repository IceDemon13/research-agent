using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class SupplierProductDto
    {
        [JsonProperty("product_id")]
        public int? ProductId { get; set; }

        [JsonProperty("supplier_id")]
        public int? SupplierId { get; set; }

        [JsonProperty("supplier_product_id")]
        public string SupplierProductId { get; set; }

        [JsonProperty("supplier_product_name")]
        public string SupplierProductName { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("pn")]
        public string Pn { get; set; }

        [JsonProperty("founded_only_in_nomenclature")]
        public bool FoundedOnlyInNomenclature { get; set; }
    }
}