using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public record UpdateExternalPaymentDto
    {
        [JsonProperty("external_order_id")]
        public string ExternalOrderId { get; init; }
    }
}