using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeIns : QueryEntitiesPagedRequestBase<TradeInDto>
    {
        public QueryTradeIns(IFilteringItem filter)
            : base(filter, ApiResources.TradeIns)
        {
        }
    }
}