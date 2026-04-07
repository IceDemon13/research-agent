using Newtonsoft.Json;

namespace Telemart.Client.TransferObjects.NovaposhtaTtn
{
    public class NpDocumentSaveDto
    {
        public NpDocumentSaveDto(string ttn, string npContractorRef, int sourceId, int payerTypeId, string comment)
        {
            Id = ttn;
            NpContractorRef = npContractorRef;
            SourceId = sourceId;
            PayerTypeId = payerTypeId;
            Comment = comment;
        }

        [JsonProperty("Id")]
        public string Id { get; }

        [JsonProperty("np_conreactor_ref")]
        public string NpContractorRef { get; }

        [JsonProperty("source_id")]
        public int SourceId { get; }

        [JsonProperty("payer_type_id")]
        public int PayerTypeId { get; }

        [JsonProperty("comment")]
        public string Comment { get; }
    }
}