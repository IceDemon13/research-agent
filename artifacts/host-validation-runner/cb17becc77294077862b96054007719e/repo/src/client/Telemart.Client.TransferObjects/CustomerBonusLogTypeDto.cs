using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class CustomerBonusLogTypeDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name_bonus_log_type")]
        public string Name { get; set; }
    }
}