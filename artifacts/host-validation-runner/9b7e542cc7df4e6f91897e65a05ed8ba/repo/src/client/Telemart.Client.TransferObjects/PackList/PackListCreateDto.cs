using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.PackList
{
    public class PackListCreateDto
    {
        [JsonProperty("time")]
        public DateTime Time { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("carry_ids")]
        public int[] CarryIds { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("packager")]
        public int PackagerEmployeeId { get; set; }

        [JsonProperty("collector")]
        public int CollectorEmployeeId { get; set; }
    }
}