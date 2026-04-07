using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed class MovementsMassScanDto
    {
        [JsonProperty("movement_ids")]
        public IReadOnlyCollection<int> MovementIds { get; init; }

        [JsonProperty("products")]
        public IReadOnlyCollection<MovementProductMassScanDto> Products { get; init; }

        [JsonProperty("additional_service_products")]
        public IReadOnlyCollection<MovementAdditionalServiceProductDto> AdditionalServiceProducts { get; init; }

        [JsonProperty("assembly_service_products")]
        public IReadOnlyCollection<MovementAssemblyServiceProductDto> AssemblyServiceProducts { get; init; }
    }
}