using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Content
{
    public sealed class QueryFeatures : QueryEntitiesPagedRequestBase<FeatureFullDto>
    {
        public QueryFeatures(IFilteringItem filter)
            : base(filter, ApiResources.Features)
        {
        }
    }
}
