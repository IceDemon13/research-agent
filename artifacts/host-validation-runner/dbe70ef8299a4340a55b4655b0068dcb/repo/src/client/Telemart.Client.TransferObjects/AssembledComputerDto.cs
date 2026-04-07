using System.Collections.Generic;
using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record AssembledComputerDto
    {
        [JsonProperty("nomenclature_series")]
        public string NomenclatureSeries { get; init; }

        [JsonProperty("order_id")]
        public int OrderId { get; init; }

        [JsonProperty("service_request_id")]
        public int? ServiceRequestId { get; init; }

        [JsonProperty("service_order_id")]
        public int? ServiceOrderId { get; init; }

        [JsonProperty("active")]
        public bool Active { get; init; }

        [JsonProperty("product_id")]
        public int? ProductId { get; init; }

        [JsonProperty("nomenclature_series_accounting")]
        public bool NomenclatureSeriesAccounting { get; init; }

        [JsonProperty("products")]
        public IReadOnlyCollection<AssembledComputerProductDto> Products { get; init; }
    }
}