using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class GenericMetabaseAccountDto
    {
        [JsonProperty("login")]
        public string Login { get; init; }

        [JsonProperty("password")]
        public string Password { get; init; }
    }
}