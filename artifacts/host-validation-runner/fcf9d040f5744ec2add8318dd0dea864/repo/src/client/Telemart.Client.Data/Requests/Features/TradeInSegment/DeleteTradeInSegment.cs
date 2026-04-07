using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.TradeInSegment
{
    public sealed class DeleteTradeInSegment : DeleteEntityResultRequestBase<object>
    {
        public DeleteTradeInSegment(int id)
            : base(ApiResources.TradeInSegments, id)
        {
        }
    }
}