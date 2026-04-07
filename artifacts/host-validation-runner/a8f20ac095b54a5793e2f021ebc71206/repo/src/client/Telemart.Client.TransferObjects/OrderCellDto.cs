using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderCellDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("cell_id")]
        public int CellId { get; set; }

        [JsonProperty("cell_name")]
        public string CellName { get; set; }

        [JsonProperty("order_id")]
        public int OrderId { get; set; }

        [JsonProperty("completed")]
        public bool Completed { get; set; }
    }
}