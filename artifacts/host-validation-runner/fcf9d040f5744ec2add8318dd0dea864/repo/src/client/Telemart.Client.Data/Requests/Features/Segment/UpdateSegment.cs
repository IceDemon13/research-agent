using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Segment;

namespace Telemart.Client.Data.Requests.Features.Segment
{
    public sealed class UpdateSegment : UpdateEntityResultRequestBase<SegmentDto, SegmentUpdateDto>
    {
        public UpdateSegment(SegmentUpdateDto dto)
            : base(dto, ApiResources.Segments, dto.Id)
        {
        }
    }
}