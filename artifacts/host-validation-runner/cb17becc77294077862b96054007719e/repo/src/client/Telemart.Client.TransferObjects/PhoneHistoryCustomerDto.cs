using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class PhoneHistoryCustomerDto
    {
        [JsonProperty("client_id")]
        public int CustomerId { get; set; }

        [JsonProperty("client_name")]
        public string CustomerName { get; set; }
    }
}