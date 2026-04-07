using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Perfomance
{
    public class WarehousePerformanceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("work_id")]
        public int WorkId { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("subdivision_id")]
        public int? SubdivisionId { get; set; }

        [JsonProperty("day_of_week")]
        public string DayOfWeek { get; set; }

        [JsonProperty("performance")]
        public string Performance { get; set; }

        [JsonProperty("additional_service_id")]
        public int? AdditionalServiceId { get; set; }

        [JsonProperty("additional_service_name")]
        public string AdditionalServiceName { get; set; }

        [JsonProperty("estimate")]
        public TimeSpan? Estimate { get; set; }

        [JsonProperty("activity")]
        public bool Activity { get; set; }
    }
}