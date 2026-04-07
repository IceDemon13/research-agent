using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Locations;

namespace Telemart.Client.Data.Requests.Features.Locations
{
    public class QueryLocation : QueryEntityRequestBase<LocationEntityDto>
    {
        public QueryLocation(int id)
            : base(ApiResources.Locations, id)
        {
        }
    }
}