using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SupplierCarrySaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("filial")]
        public string Filial { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("time_get")]
        public TimeSpan TimeGet { get; set; }

        [JsonProperty("time_get_st")]
        public TimeSpan TimeGetSt { get; set; }

        [JsonProperty("time_close")]
        public TimeSpan TimeClose { get; set; }

        [JsonProperty("time_close_st")]
        public TimeSpan TimeCloseSt { get; set; }

        [JsonProperty("time_arrive")]
        public TimeSpan TimeArrive { get; set; }

        [JsonProperty("time_arrive_st")]
        public TimeSpan TimeArriveSt { get; set; }

        [JsonProperty("days")]
        public int Days { get; set; }

        [JsonProperty("supplier_warehouse_id")]
        public int? SupplierWarehouseId { get; set; }
    }
}