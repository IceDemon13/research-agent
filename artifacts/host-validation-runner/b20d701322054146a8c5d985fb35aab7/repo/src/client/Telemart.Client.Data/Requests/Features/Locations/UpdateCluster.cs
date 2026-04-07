using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;
using Telemart.Common.ErrorHandling;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public sealed class UpdateCluster : UpdateEntityRequestBase<Result<ClusterDto>, ClusterDto>
    {
        public UpdateCluster(ClusterDto dto)
            : base(dto, ApiResources.Locations, dto.Id, "update_cluster")
        {
        }
    }
}