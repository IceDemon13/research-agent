using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class AuditEntryDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("created_by")]
        public int? CreatedBy { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("entity_id")]
        public string EntityId { get; set; }

        [JsonProperty("entity_set_name")]
        public string EntitySetName { get; set; }

        [JsonProperty("entity_type_name")]
        public string EntityTypeName { get; set; }

        [JsonProperty("properties")]
        public List<AuditEntryPropertyDto> Properties { get; set; }

        [JsonProperty("state_name")]
        public string StateName { get; set; }
    }
}