using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeInCancelReasons : QueryEntitiesRequestBase<TradeInCancelReasonDto>
    {
        public QueryTradeInCancelReasons()
            : base($"{ApiResources.TradeIns}/cancel_reasons")
        {
        }
    }
}