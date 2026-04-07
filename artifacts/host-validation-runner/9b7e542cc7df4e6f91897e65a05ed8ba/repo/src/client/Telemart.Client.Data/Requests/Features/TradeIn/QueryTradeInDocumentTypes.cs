using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeInDocumentTypes : QueryEntitiesRequestBase<TradeInDocumentTypeDto>
    {
        public QueryTradeInDocumentTypes()
            : base($"{ApiResources.TradeIns}/document_types")
        {
        }
    }
}