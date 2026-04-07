using Newtonsoft.Json;
using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public class CreateCluster : CreateEntityResultRequestBase<ClusterDto, CreateCluster.CreateClusterDto>
    {
        public CreateCluster(string name, int[] locationIds)
            : base(new CreateClusterDto(name, locationIds), ApiResources.Locations, "create_cluster")
        {
        }

        public sealed class CreateClusterDto
        {
            public CreateClusterDto(string name, int[] locationIds)
            {
                LocationIds = locationIds;
                Name = name;
            }

            [JsonProperty("name")]
            public string Name { get; }

            [JsonProperty("location_ids")]
            public int[] LocationIds { get; }
        }
    }
}