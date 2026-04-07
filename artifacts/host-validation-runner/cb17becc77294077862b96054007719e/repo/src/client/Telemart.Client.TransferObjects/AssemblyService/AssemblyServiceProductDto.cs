using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.AssemblyService
{
    public sealed class AssemblyServiceProductDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("assembly_service_id")]
        public int AssemblyServiceId { get; set; }

        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("order_product_id")]
        public int? OrderProductId { get; set; }

        [JsonProperty("category_type_id")]
        public int CategoryTypeId { get; set; }

        [JsonProperty("serial_numbers")]
        public List<string> SerialNumbers { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("name_ukr")]
        public string NameUkr { get; set; }

        [JsonProperty("name_en")]
        public string NameEn { get; set; }

        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("scanned_quantity")]
        public int ScannedQuantity { get; set; }

        [JsonProperty("keep_serial")]
        public bool KeepSerial { get; set; }

        [JsonProperty("order_folder_id")]
        public int? OrderFolderId { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }
    }
}