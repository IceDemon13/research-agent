using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeInDocument : QueryEntityRequestBase<TradeInDocumentDto>
    {
        public QueryTradeInDocument(int id)
            : base($"{ApiResources.TradeIns}/documents/{id}")
        {
        }
    }
}