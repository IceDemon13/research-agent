using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ScheduleDeliveryOrderDto
    {
        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("courier_employee_id")]
        public int CourierEmployeeId { get; init; }

        [JsonProperty("delivery_time")]
        public DateTime DeliveryTime { get; init; }

        [JsonProperty("delivery_time_to")]
        public DateTime DeliveryTimeTo { get; init; }

        [JsonProperty("comment")]
        public string Comment { get; init; }
    }
}