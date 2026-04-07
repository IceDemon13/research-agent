using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderLogisticsResultDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("carry_id")]
        public int CarryId { get; set; }

        [JsonProperty("subdivision_id")]
        public int SubdivisionId { get; set; }

        [JsonProperty("delivery_time")]
        public DateTime? DeliveryTime { get; set; }

        [JsonProperty("delivery_time_to")]
        public DateTime? DeliveryTimeTo { get; set; }

        [JsonProperty("total_additional_service_estimate")]
        public TimeSpan? TotalAdditionalServiceEstimate { get; set; }

        [JsonProperty("assembly_dates")]
        public IReadOnlyCollection<DateTime> AssemblyDates { get; set; }

        [JsonProperty("additional_service_dates")]
        public IReadOnlyCollection<DateTime> AdditionalServiceDates { get; set; }

        [JsonProperty("additional_service_quotas")]
        public IReadOnlyCollection<OrderLogisticsAddіtionalServiceQuotaDto> AdditionalServiceQuotas { get; set; }

        [JsonProperty("products")]
        public IReadOnlyCollection<OrderProductLogisticsResultDto> OrderProducts { get; set; }
    }
}