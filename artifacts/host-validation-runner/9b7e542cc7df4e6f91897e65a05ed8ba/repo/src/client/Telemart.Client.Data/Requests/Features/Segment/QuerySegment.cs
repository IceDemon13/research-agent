using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Segment;

namespace Telemart.Client.Data.Requests.Features.Segment
{
    public sealed class QuerySegment : QueryEntityRequestBase<SegmentDto>
    {
        public QuerySegment(int id)
            : base(ApiResources.Segments, id)
        {
        }
    }
}