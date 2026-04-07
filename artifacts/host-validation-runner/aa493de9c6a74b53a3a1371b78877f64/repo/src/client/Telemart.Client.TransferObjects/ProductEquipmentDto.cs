using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class ProductEquipmentDto
    {
        [JsonProperty("product_id")]
        public int ProductId { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("equipment")]
        public string Equipment { get; set; }

        [JsonProperty("equipment_ukr")]
        public string EquipmentUkr { get; set; }

        [JsonProperty("equipment_en")]
        public string EquipmentEn { get; set; }
    }
}