using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public class QueryCluster : QueryEntityRequestBase<ClusterDto>
    {
        public QueryCluster(int id)
            : base(ApiResources.Locations, id, "cluster")
        {
        }
    }
}