using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record FastCompleteServiceRequestProccesingDto
    {
        [JsonProperty("service_request_id")]
        public int ServiceRequestId { get; init; }

        [JsonProperty("apppearance")]
        public string Appearance { get; init; }

        [JsonProperty("inspection")]
        public string Inspection { get; init; }

        [JsonProperty("sn")]
        public string SerialNumber { get; init; }

        [JsonProperty("additional_service_warehouse_id")]
        public int AdditionalServiceWarehouseId { get; init; }
    }
}