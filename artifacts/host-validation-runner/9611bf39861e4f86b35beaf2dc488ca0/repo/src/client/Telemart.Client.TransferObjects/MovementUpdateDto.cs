using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record MovementUpdateDto
    {
        [JsonProperty("products")]
        public MovementProductSaveDto[] Products { get; init; }

        [JsonProperty("additional_service_products")]
        public MovementAdditionalServiceProductDto[] AdditionalServiceProducts { get; init; }

        [JsonProperty("assembly_service_products")]
        public MovementAssemblyServiceProductDto[] AssemblyServiceProducts { get; init; }
    }
}