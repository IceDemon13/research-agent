using Telemart.Client.Data.Requests.Base.Action;
using Telemart.Client.TransferObjects.Segment;

namespace Telemart.Client.Data.Requests.Features.Segment.Actions
{
    public sealed class RecalculateSegments : CallActionWithBodyRequestResultBase<RecalculateSegmentsResultDto, RecalculateSegmentsDto>
    {
        public RecalculateSegments(RecalculateSegmentsDto dto)
            : base(dto, ApiResources.Segments, "recalculate")
        {
        }
    }
}