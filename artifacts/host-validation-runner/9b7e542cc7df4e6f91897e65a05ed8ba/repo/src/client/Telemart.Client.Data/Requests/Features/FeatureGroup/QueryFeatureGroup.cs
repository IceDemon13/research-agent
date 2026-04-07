using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.FeatureGroup
{
    public class QueryFeatureGroup : QueryEntityRequestBase<FeatureGroupSimpleDto>
    {
        public QueryFeatureGroup(object id)
            : base(ApiResources.FeaturesGroups, id)
        {
        }
    }
}