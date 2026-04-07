using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class SupplierCarryDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("filial")]
        public string Filial { get; init; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; init; }

        [JsonProperty("carry_id")]
        public int CarryId { get; init; }

        [JsonProperty("time_get")]
        public TimeSpan TimeGet { get; init; }

        [JsonProperty("time_get_st")]
        public TimeSpan TimeGetSt { get; init; }

        [JsonProperty("time_close")]
        public TimeSpan TimeClose { get; init; }

        [JsonProperty("time_close_st")]
        public TimeSpan TimeCloseSt { get; init; }

        [JsonProperty("time_arrive")]
        public TimeSpan TimeArrive { get; init; }

        [JsonProperty("time_arrive_st")]
        public TimeSpan TimeArriveSt { get; init; }

        [JsonProperty("days")]
        public int Days { get; init; }

        [JsonProperty("supplier_warehouse_id")]
        public int? SupplierWarehouseId { get; init; }

        [JsonProperty("main")]
        public bool Main { get; init; }
    }
}