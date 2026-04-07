using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class WorkPlaceCreateDto
    {
        public WorkPlaceCreateDto(int typeId, int deviceTypeId, string uniqueDeviceId)
        {
            TypeId = typeId;
            DeviceTypeId = deviceTypeId;
            UniqueDeviceId = uniqueDeviceId;
        }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("device_type_id")]
        public int DeviceTypeId { get; set; }

        [JsonProperty("unique_device_id")]
        public string UniqueDeviceId { get; set; }
    }
}