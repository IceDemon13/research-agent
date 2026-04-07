using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public class QueryExternalLocations : QueryEntitiesRequestBase<ExternalLocationDto>
    {
        public QueryExternalLocations()
            : base(ApiResources.Locations, "external_locations")
        {
        }
    }
}