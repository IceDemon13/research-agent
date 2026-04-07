using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ProductAdditionalServiceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("group_id")]
        public int GroupId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("product_name")]
        public string ProductName { get; set; }

        [JsonProperty("product_name_ukr")]
        public string ProductNameUkr { get; set; }

        [JsonProperty("product_name_en")]
        public string ProductNameEn { get; set; }

        [JsonProperty("product_link")]
        public string ProductLink { get; set; }

        [JsonProperty("min_price")]
        public decimal MinPrice { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("percent")]
        public decimal Percent { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("product_type_id")]
        public int ProductTypeId { get; set; }

        [JsonProperty("assembly_part")]
        public bool AssemblyPart { get; set; }

        [JsonProperty("auto_add")]
        public bool AutoAdd { get; set; }

        [JsonProperty("additional_warranty")]
        public bool AdditionalWarranty { get; set; }

        [JsonProperty("product")]
        public ProductDto Product { get; set; }
    }
}