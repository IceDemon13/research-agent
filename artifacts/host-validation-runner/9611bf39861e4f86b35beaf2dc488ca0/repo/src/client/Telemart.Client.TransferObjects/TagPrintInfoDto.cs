using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class TagPrintInfoDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("date_time")]
        public DateTime DateTime { get; set; }

        [JsonProperty("warehouse")]
        public WarehouseSimpleDto Warehouse { get; set; }
    }
}