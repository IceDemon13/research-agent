using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.TradeInSegment;

namespace Telemart.Client.Data.Requests.Features.TradeInSegment.Actions
{
    public sealed class RecalculateTradeInSegments : CallActionWithBodyRequestResultBase<RecalculateTradeInSegmentsResultDto, RecalculateTradeInSegmentsDto>
    {
        public RecalculateTradeInSegments(RecalculateTradeInSegmentsDto dto)
            : base(dto, ApiResources.TradeInSegments, "recalculate")
        {
        }
    }
}