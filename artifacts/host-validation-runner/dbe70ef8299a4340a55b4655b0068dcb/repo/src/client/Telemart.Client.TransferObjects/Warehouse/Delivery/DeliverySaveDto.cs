using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Delivery
{
    public class DeliverySaveDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("carry_ids")]
        public int[] CarryIds { get; set; }

        [JsonProperty("warehouse_id")]
        public int WarehouseId { get; set; }

        [JsonProperty("subdivision_id")]
        public int? SubdivisionId { get; set; }

        [JsonProperty("days_of_week")]
        public string DaysOfWeek { get; set; }

        [JsonProperty("days")]
        public int Days { get; set; }

        [JsonProperty("plan_courier_call")]
        public bool PlanCourierCall { get; set; }

        [JsonProperty("planned_weight")]
        public int? PlannedWeight { get; set; }

        [JsonProperty("entity_type_ids")]
        public IReadOnlyCollection<int> EntityTypeIds { get; set; }

        [JsonProperty("time_get")]
        public TimeSpan TimeGet { get; set; }

        [JsonProperty("time_delivery_from")]
        public TimeSpan TimeDeliveryFrom { get; set; }

        [JsonProperty("time_delivery_to")]
        public TimeSpan TimeDeliveryTo { get; set; }
    }
}