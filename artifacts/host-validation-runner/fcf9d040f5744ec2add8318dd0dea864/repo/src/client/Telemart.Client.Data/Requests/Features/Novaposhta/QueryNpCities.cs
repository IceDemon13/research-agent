using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Novaposhta;

namespace Telemart.Client.Data.Requests.Features.Novaposhta
{
    public class QueryNpCities : QueryEntitiesRequestBase<NpCityDto>
    {
        public QueryNpCities(IFilteringItem filter)
            : base(filter, $"{ApiResources.Novaposhta}/cities")
        {
        }
    }

    public class NpCitiesFilter : FilteringItemBase
    {
        public NpCitiesFilter(string areaRef)
        {
            Area = areaRef;
        }

        [FilteringItemProperty("area_ref")]
        public string Area { get; }
    }
}