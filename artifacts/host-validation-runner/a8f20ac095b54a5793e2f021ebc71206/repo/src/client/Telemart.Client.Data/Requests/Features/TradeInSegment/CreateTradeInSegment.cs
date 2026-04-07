using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.TradeInSegment;

namespace Telemart.Client.Data.Requests.Features.TradeInSegment
{
    public sealed class CreateTradeInSegment : CreateEntityResultRequestBase<TradeInSegmentDto, TradeInSegmentCreateDto>
    {
        public CreateTradeInSegment(TradeInSegmentCreateDto dto)
            : base(dto, ApiResources.TradeInSegments)
        {
        }
    }
}