using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Content;

namespace Telemart.Client.Data.Requests.Features.Content
{
    public sealed class QueryFeature : QueryEntityRequestBase<FeatureFullDto>
    {
        public QueryFeature(int id)
            : base(ApiResources.Features, id)
        {
        }
    }
}