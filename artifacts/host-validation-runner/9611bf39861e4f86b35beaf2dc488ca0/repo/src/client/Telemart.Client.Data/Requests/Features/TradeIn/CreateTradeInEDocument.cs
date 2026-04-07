using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public class CreateTradeInEDocument : CreateEntityResultRequestBase<TradeInEDocumentDto, CreateTradeInEDocument.CreateTradeInEDocumentDto>
    {
        public CreateTradeInEDocument(int tradeInId, byte[] fileBytes)
            : base(new CreateTradeInEDocumentDto() { TradeInId = tradeInId, FileBytes = fileBytes }, ApiResources.TradeIns, tradeInId, "e_document")
        {
        }

        public sealed class CreateTradeInEDocumentDto
        {
            [JsonProperty("trade_in_id")]
            public int TradeInId { get; init; }

            [JsonProperty("file_bytes")]
            public byte[] FileBytes { get; init; }
        }
    }
}