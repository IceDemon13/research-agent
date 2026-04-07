using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Warehouse.Route
{
    public sealed record WarehouseRouteTimeCreateDto
    {
        [JsonProperty("time_out")]
        public TimeSpan TimeOut { get; init; }

        [JsonProperty("time_in")]
        public TimeSpan TimeIn { get; init; }

        [JsonProperty("time_departure")]
        public TimeSpan TimeDeparture { get; init; }

        [JsonProperty("time_arrive")]
        public TimeSpan TimeArrive { get; init; }

        [JsonProperty("days")]
        public int Days { get; init; }

        [JsonProperty("days_of_week_out")]
        public string DaysOfWeekOut { get; init; }

        [JsonProperty("days_of_week_in")]
        public string DaysOfWeekIn { get; init; }

        [JsonProperty("carry_id")]
        public int? CarryId { get; init; }

        [JsonProperty("delivery_type_id")]
        public int? DeliveryTypeId { get; init; }

        [JsonProperty("auto")]
        public bool Auto { get; init; }

        [JsonProperty("delivery_type")]
        public DeliveryTypeDto DeliveryType { get; init; }

        [JsonProperty("purpose_ids")]
        public IReadOnlyCollection<int> PurposeIds { get; init; }
    }
}