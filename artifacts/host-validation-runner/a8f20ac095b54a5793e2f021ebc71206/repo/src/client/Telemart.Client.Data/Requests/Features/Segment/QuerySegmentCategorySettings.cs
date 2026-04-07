using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Segment;

namespace Telemart.Client.Data.Requests.Features.Segment
{
    public sealed class QuerySegmentCategorySettings : QueryEntitiesRequestBase<SegmentCategorySettingsDto>
    {
        public QuerySegmentCategorySettings()
            : base($"{ApiResources.Segments}/actions/category_settings")
        {
        }
    }
}