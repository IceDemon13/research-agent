using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.City;

namespace Telemart.Client.Data.Requests.Features.City
{
    public class QueryForeignCities : QueryEntitiesRequestBase<ForeignCityDto>
    {
        public QueryForeignCities()
            : base($"{ApiResources.Cities}/foreign")
        {
        }
    }
}