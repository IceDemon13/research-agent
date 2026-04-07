using System;
using Telemart.Client.Data.Requests.Base.Action;

namespace Telemart.Client.Data.Requests.Features.Segment.Actions
{
    public sealed class RecalculateAbcProductCategorySegments : CallActionRequestResultBase<object>
    {
        public RecalculateAbcProductCategorySegments()
            : base(ApiResources.Segments, "recalculate_abc_product_category")
        {
            DefaultTimeout = TimeSpan.FromMinutes(2);
        }
    }
}