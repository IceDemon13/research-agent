using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class WarehouseTagFormatDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_id")]
        public int? WarehouseId { get; set; }

        [JsonProperty("tag_format_id")]
        public int TagFormatId { get; set; }

        [JsonProperty("width")]
        public double Width { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }
    }
}