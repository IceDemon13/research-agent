using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Segment.Actions
{
    public sealed class RecalculateAbcSegments : CallActionRequestResultBase<object>
    {
        public RecalculateAbcSegments()
            : base(ApiResources.Segments, "recalculate_abc")
        {
        }
    }
}