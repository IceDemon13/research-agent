using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record AssembledComputerSaveDto
    {
        [JsonProperty("product_id")]
        public int? ProductId { get; init; }

        [JsonProperty("products")]
        public IReadOnlyCollection<AssembledComputerProductSaveDto> Products { get; init; }
    }
}