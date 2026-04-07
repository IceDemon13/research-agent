using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record MovementAssembledComputerDto
    {
        [JsonProperty("nomenclature_series")]
        public string NomenclatureSeries { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("product_id")]
        public int ProductId { get; init; }
    }
}