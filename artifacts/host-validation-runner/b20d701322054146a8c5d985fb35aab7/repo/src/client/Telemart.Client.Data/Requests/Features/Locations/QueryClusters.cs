using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public sealed class QueryClusters : QueryEntitiesRequestBase<ClusterDto>
    {
        public QueryClusters()
            : base(ApiResources.Locations, "clusters")
        {
        }
    }
}