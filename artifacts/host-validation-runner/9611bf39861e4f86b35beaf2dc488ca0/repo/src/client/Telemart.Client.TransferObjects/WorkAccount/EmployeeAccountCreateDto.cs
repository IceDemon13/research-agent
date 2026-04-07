using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkAccount
{
    public class EmployeeAccountCreateDto
    {
        [JsonProperty("account_id")]
        public int AccountId { get; init; }

        [JsonProperty("login")]
        public string Login { get; init; }

        [JsonProperty("password")]
        public string Password { get; init; }
    }
}