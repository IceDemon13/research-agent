using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Locations
{
    public sealed record LocationEntityDto
    {
        public LocationEntityDto(
            string name,
            int cityId,
            string address,
            int? workScheduleTypeId,
            int? additionalScheduleTypeId,
            int locationTypeId,
            bool syncDeliverySchedules,
            int? googleLocationId,
            int? clusterId)
        {
            Name = name;
            CityId = cityId;
            Address = address;
            WorkScheduleTypeId = workScheduleTypeId;
            AdditionalScheduleTypeId = additionalScheduleTypeId;
            SyncDeliverySchedules = syncDeliverySchedules;
            LocationTypeId = locationTypeId;
            GoogleExternalLocationId = googleLocationId;
            ClusterId = clusterId;
        }

        public LocationEntityDto()
        {
        }

        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("city_id")]
        public int CityId { get; init; }

        [JsonProperty("address")]
        public string Address { get; init; }

        [JsonProperty("work_schedule_type_id")]
        public int? WorkScheduleTypeId { get; init; }

        [JsonProperty("additional_work_schedule_type_id")]
        public int? AdditionalScheduleTypeId { get; init; }

        [JsonProperty("location_type_id")]
        public int LocationTypeId { get; init; }

        [JsonProperty("warehouse_ids")]
        public int[] WarehouseIds { get; init; }

        [JsonProperty("sync_delivery_schedules")]
        public bool SyncDeliverySchedules { get; init; }

        [JsonProperty("google_external_location_id")]
        public int? GoogleExternalLocationId { get; init; }

        [JsonProperty("cluster_id")]
        public int? ClusterId { get; init; }
    }
}