using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeInIndicatorValues : QueryEntitiesRequestBase<TradeInIndicatorValueDto>
    {
        public QueryTradeInIndicatorValues()
            : base($"{ApiResources.TradeIns}/indicator_values")
        {
        }
    }
}