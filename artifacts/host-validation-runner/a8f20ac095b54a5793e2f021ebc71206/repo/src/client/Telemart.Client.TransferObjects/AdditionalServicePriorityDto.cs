using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServicePriorityDto
    {
        public AdditionalServicePriorityDto(int additionalServiceId, int priority)
        {
            AdditionalServiceId = additionalServiceId;
            Priority = priority;
        }

        [JsonProperty("additional_service_id")]
        public int AdditionalServiceId { get; set; }

        [JsonProperty("priority")]
        public int Priority { get; set; }
    }
}