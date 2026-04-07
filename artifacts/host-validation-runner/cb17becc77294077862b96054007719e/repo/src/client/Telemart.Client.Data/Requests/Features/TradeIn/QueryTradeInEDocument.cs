using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public class QueryTradeInEDocument : QueryEntityRequestBase<TradeInEDocumentDto>
    {
        public QueryTradeInEDocument(int documentId)
            : base(ApiResources.TradeIns, "e_document", documentId)
        {
        }
    }
}