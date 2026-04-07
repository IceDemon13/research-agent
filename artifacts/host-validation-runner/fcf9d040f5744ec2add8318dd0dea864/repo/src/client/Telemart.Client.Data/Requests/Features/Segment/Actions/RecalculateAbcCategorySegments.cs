using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Segment.Actions
{
    public sealed class RecalculateAbcCategorySegments : CallActionRequestResultBase<object>
    {
        public RecalculateAbcCategorySegments()
            : base(ApiResources.Segments, "recalculate_abc_category")
        {
        }
    }
}