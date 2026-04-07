using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeInDocuments : QueryEntitiesRequestBase<TradeInDocumentSimpleDto>
    {
        public QueryTradeInDocuments(int tradeInId)
            : base($"{ApiResources.TradeIns}/{tradeInId}/documents")
        {
        }
    }
}