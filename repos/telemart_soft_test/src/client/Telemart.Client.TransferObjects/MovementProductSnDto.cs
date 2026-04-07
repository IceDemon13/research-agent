using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects
{
    public sealed record MovementProductSnDto
    {
        [JsonProperty("id")]
        public int Id { get; init; }

        [JsonProperty("sn")]
        public string Sn { get; init; }

        [JsonProperty("scanned_in")]
        public bool ScannedIn { get; init; }

        [JsonProperty("scanned_out")]
        public bool ScannedOut { get; init; }

        [JsonProperty("nomenclature_series")]
        public string NomenclatureSeries { get; init; }
    }
}