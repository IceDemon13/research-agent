using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkAccount
{
    public class EmployeeAccountDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("account_id")]
        public int AccountId { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }

        [JsonProperty("account_info")]
        public AccountInfoDto AccountInfoDto { get; set; }
    }
}