using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Bitrix
{
    public class BitrixTaskDto
    {
        [JsonProperty("bitrix_id")]
        public int BitrixId { get; set; }

        [JsonProperty("priority_id")]
        public int PriorityId { get; set; }

        [JsonProperty("role_id")]
        public int? RoleId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("created_by_name")]
        public string CreatedByName { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("deadline")]
        public DateTime? Deadline { get; set; }
    }
}