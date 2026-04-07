using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeIn;

namespace Telemart.Client.Data.Requests.Features.TradeIn
{
    public sealed class QueryTradeInTrackNumberSources : QueryEntitiesRequestBase<TradeInCreateTrackNumberSourceDto>
    {
        public QueryTradeInTrackNumberSources(int tradeInId)
        : base(ApiResources.TradeIns, tradeInId, "tnsources")
        {
        }
    }
}