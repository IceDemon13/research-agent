using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public class OrderStateChangeReasonDto
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("parent_id")]
        public int? ParentId { get; set; }

        [JsonProperty("state_id")]
        public int? StateId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public int Position { get; set; }

        [JsonProperty("only_site")]
        public bool OnlySite { get; set; }

        [JsonProperty("only_additional_service")]
        public bool OnlyAdditionalService { get; set; }

        [JsonProperty("only_assembly_service")]
        public bool OnlyAssemblyService { get; set; }
    }
}