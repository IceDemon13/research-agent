using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeInSegment;

namespace Telemart.Client.Data.Requests.Features.TradeInSegment
{
    public sealed class QueryTradeInSegments : QueryEntitiesRequestBase<TradeInSegmentDto>
    {
        public QueryTradeInSegments()
            : base(ApiResources.TradeInSegments)
        {
        }
    }
}