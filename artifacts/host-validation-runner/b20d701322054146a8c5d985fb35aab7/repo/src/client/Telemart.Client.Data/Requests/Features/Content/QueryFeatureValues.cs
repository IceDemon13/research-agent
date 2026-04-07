using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Content
{
    public class QueryFeatureValues : QueryEntitiesRequestBase<FeatureValueExDto>
    {
        public QueryFeatureValues(int featureId)
            : base(ApiResources.Features, featureId, "values")
        {
        }
    }
}