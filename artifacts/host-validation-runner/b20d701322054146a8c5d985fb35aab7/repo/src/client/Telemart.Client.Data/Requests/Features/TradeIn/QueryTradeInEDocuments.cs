using System.Collections.Generic;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public class QueryTradeInEDocuments : QueryEntityRequestBase<Result<IReadOnlyCollection<TradeInEDocumentSimpleDto>>>
    {
        public QueryTradeInEDocuments(int tradeInId)
            : base(ApiResources.TradeIns, tradeInId, "e_documents")
        {
        }
    }
}