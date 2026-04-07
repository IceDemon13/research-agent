using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeInSegment;

namespace Telemart.Client.Data.Requests.Features.TradeInSegment
{
    public sealed class UpdateTradeInSegment : UpdateEntityResultRequestBase<TradeInSegmentDto, TradeInSegmentUpdateDto>
    {
        public UpdateTradeInSegment(TradeInSegmentUpdateDto dto)
            : base(dto, ApiResources.TradeInSegments, dto.Id)
        {
        }
    }
}