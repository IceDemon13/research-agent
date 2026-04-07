using System;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class ServiceRequestDiscussionDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("created_on")]
        public DateTime CreatedOn { get; set; }

        [JsonProperty("created_by")]
        public int CreatedBy { get; set; }

        [JsonProperty("by_service_manager")]
        public bool ByServiceManager { get; set; }
    }
}