using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Call
{
    public sealed class CallDependencyDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("call_id")]
        public int CallId { get; set; }

        [JsonProperty("dependency_type_id")]
        public int? DependencyTypeId { get; set; }

        [JsonProperty("document_id")]
        public int? DocumentId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("approved")]
        public bool Approved { get; set; }

        [JsonProperty("system")]
        public bool System { get; set; }
    }
}