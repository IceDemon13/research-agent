using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.TradeIn
{
    public sealed record TradeInActReportDto
    {
        [JsonProperty("legal_entity_name")]
        public string LegalEntityName { get; init; }

        [JsonProperty("legal_entity_requisites")]
        public string LegalEntityRequisites { get; init; }
    }
}