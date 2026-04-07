using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public sealed class QueryLocations : QueryEntitiesRequestBase<LocationEntityDto>
    {
        public QueryLocations()
            : base(ApiResources.Locations)
        {
        }
    }
}