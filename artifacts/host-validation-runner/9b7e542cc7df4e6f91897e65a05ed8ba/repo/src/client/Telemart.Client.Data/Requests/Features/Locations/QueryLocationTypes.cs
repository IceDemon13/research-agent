using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public sealed class QueryLocationTypes : QueryEntitiesRequestBase<LocationTypeEntityDto>
    {
        public QueryLocationTypes()
        : base(ApiResources.Locations, "location_types")
        {
        }
    }
}