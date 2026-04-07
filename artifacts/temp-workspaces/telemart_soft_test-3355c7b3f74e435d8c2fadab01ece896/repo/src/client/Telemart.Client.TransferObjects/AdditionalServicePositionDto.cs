using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AdditionalServicePositionDto
    {
        public AdditionalServicePositionDto(int additionalServiceId, int position)
        {
            AdditionalServiceId = additionalServiceId;
            Position = position;
        }

        [JsonProperty("additional_service_id")]
        public int AdditionalServiceId { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }
    }
}