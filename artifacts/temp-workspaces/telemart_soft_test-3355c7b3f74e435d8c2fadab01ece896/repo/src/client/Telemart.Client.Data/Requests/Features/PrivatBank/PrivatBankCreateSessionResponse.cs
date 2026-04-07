using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.PrivatBank
{
    public class PrivatBankCreateSessionResponse
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("state_id")]
        public int StateId { get; set; }

        [JsonProperty("phones")]
        public PrivatBankPhoneDto[] Phones { get; set; }
    }
}