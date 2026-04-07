using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Content
{
    public sealed class QueryFeatureOptions : QueryEntitiesRequestBase<FeatureOptionDto>
    {
        public QueryFeatureOptions()
            : base(ApiResources.Features, "feature_options")
        {
        }
    }
}