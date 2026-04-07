using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.ServiceMovement
{
    public class ServiceMovementReceiveDto
    {
        [JsonProperty("product_ids")]
        public int[] ProductIds { get; set; }
    }
}