using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeInSegment;

namespace Telemart.Client.Data.Requests.Features.TradeInSegment
{
    public sealed class QueryTradeInSegment : QueryEntityRequestBase<TradeInSegmentDto>
    {
        public QueryTradeInSegment(int id)
            : base(ApiResources.TradeInSegments, id)
        {
        }
    }
}