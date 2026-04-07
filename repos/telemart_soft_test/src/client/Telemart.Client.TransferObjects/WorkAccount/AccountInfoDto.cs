using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.WorkAccount
{
    public sealed class AccountInfoDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("role_ids")]
        public int[] RoleIds { get; set; }

        [JsonProperty("create_on")]
        public DateTime? CreateOn { get; set; }
    }
}