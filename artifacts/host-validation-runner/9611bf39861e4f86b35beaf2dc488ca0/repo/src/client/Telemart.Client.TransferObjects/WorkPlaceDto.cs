using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class WorkPlaceDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("device_type_id")]
        public int DeviceTypeId { get; set; }

        [JsonProperty("unique_device_id")]
        public string UniqueDeviceId { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }
    }
}