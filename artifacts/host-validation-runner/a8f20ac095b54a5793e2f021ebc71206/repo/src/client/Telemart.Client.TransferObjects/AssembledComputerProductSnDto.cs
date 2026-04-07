using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record AssembledComputerProductSnDto
    {
        [JsonProperty("sn")]
        public string Sn { get; init; }

        [JsonProperty("assembled_computer_product_id")]
        public int AssembledComputerProductId { get; init; }
    }
}