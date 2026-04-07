using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class AssemblyTestResultDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("assembly_service_id")]
        public int AssemblyServiceId { get; set; }

        [JsonProperty("test_id")]
        public int TestId { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }
    }
}