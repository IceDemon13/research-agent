using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Country;

namespace Telemart.Client.Data.Requests.Features.Country
{
    public class QueryCountries : QueryEntitiesRequestBase<CountryDto>
    {
        public QueryCountries()
            : base($"{ApiResources.Countries}")
        {
        }
    }
}