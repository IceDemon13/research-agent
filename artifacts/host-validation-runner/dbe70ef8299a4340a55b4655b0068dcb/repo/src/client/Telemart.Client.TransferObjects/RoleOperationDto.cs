using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class RoleOperationDto
    {
        [JsonProperty("role_id")]
        public int RoleId { get; set; }

        [JsonProperty("operation_id")]
        public int OperationId { get; set; }
    }
}