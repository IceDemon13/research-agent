using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestDocumentSimpleDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("type_id")]
        public int TypeId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ext")]
        public string Ext { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("service_request_id")]
        public int ServiceRequestId { get; set; }

        [JsonIgnore]
        public bool TradeInDocument { get; init; }

        [JsonIgnore]
        public bool TradeInEDocument { get; init; }
    }
}