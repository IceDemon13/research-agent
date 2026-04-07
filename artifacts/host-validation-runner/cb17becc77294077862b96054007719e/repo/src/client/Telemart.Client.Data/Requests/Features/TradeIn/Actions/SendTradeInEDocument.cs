using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn.Actions
{
    public class SendTradeInEDocument : CallEntityActionRequestResultBase<TradeInEDocumentSimpleDto>
    {
        public SendTradeInEDocument(int id)
            : base(id, ApiResources.TradeIns, "e_document_send")
        {
        }
    }
}