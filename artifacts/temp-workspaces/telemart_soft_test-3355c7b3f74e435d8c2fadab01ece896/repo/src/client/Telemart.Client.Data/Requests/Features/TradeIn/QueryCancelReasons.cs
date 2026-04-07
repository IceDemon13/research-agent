using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryCancelReasons : QueryEntitiesRequestBase<TradeInIndicatorValueDto>
    {
        public QueryCancelReasons()
            : base($"{ApiResources.TradeIns}/cancel_reasons")
        {
        }
    }
}