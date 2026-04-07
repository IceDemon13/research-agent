using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.Teks
{
    public sealed record CreateTeksDocumentDto
    {
        [JsonProperty("movement_id")]
        public int MovementId { get; init; }

        [JsonProperty("packages")]
        public TeksPackageDto[] TeksPackages { get; init; }
    }
}