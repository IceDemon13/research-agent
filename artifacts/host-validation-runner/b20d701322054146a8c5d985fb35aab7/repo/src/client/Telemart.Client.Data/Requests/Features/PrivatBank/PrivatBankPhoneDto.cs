using Newtonsoft.Json;

namespace Telemart.Client.Data.Requests.Features.PrivatBank
{
    public class PrivatBankPhoneDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }
    }
}