using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Data.Requests.Features.City
{
    public class QueryCity : QueryEntityRequestBase<CityDto>
    {
        public QueryCity(object id)
            : base(ApiResources.Cities, id)
        {
        }
    }
}