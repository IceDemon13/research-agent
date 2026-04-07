using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public sealed class CreateLocation : CreateEntityResultRequestBase<LocationEntityDto, CreateLocation.LocationCreateDto>
    {
        public CreateLocation(string name, string address, int? cityId, int? workScheduleTypeId,  int? additionalScheduleTypeId, int locationTypeId, int? clusterId)
            : base(new LocationCreateDto(name, cityId, address, workScheduleTypeId, additionalScheduleTypeId, locationTypeId, clusterId), ApiResources.Locations)
        {
        }

        public sealed record LocationCreateDto
        {
            public LocationCreateDto(string name, int? cityId, string address, int? workScheduleTypeId,  int? additionalScheduleTypeId, int locationTypeId, int? clusterId)
            {
                Name = name;
                CityId = cityId;
                Address = address;
                WorkScheduleTypeId = workScheduleTypeId;
                AdditionalScheduleTypeId = additionalScheduleTypeId;
                LocationTypeId = locationTypeId;
                ClusterId = clusterId;
            }

            [JsonProperty("name")]
            public string Name { get; init; }

            [JsonProperty("city_id")]
            public int? CityId { get; init; }

            [JsonProperty("address")]
            public string Address { get; init; }

            [JsonProperty("work_schedule_type_id")]
            public int? WorkScheduleTypeId { get; init; }

            [JsonProperty("additional_work_schedule_type_id")]
            public int? AdditionalScheduleTypeId { get; set; }

            [JsonProperty("location_type_id")]
            public int LocationTypeId { get; set; }

            [JsonProperty("cluster_id")]
            public int? ClusterId { get; set; }
        }
    }
}