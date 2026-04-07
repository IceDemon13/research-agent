using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record ProductTypeEntityDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("name")]
        public string Name { get; init; }

        [JsonProperty("is_virtual")]
        public bool IsVirtual { get; init; }

        [JsonProperty("allowed_in_assembled_computer_rule")]
        public bool AllowedInAssembledComputerRule { get; init; }
    }
}
