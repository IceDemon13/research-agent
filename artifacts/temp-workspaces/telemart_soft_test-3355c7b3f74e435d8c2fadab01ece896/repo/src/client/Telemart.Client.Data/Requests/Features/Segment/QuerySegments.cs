using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Segment;

namespace Telemart.Client.Data.Requests.Features.Segment
{
    public sealed class QuerySegments : QueryEntitiesRequestBase<SegmentDto>
    {
        public QuerySegments()
            : base(ApiResources.Segments)
        {
        }
    }
}