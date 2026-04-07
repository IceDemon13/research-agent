using Telemart.Client.Data.Requests.Base;

namespace Telemart.Client.Data.Requests.Features.Segment
{
    public sealed class DeleteSegment : DeleteEntityResultRequestBase<object>
    {
        public DeleteSegment(int segmentId)
            : base(ApiResources.Segments, segmentId)
        {
        }
    }
}