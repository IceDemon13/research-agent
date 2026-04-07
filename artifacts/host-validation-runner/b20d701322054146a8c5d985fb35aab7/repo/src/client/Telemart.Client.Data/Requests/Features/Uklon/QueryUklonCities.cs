using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.Uklon
{
    public class QueryUklonCities : QueryEntitiesRequestBase<UklonCityDto>
    {
        public QueryUklonCities()
            : base($"{ApiResources.Uklon}/cities")
        {
        }
    }
}