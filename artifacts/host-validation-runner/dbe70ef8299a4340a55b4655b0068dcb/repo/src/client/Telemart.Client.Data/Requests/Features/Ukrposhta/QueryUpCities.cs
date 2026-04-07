using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.Ukrposhta;

namespace Telemart.Client.Data.Requests.Features.Ukrposhta
{
    public sealed class QueryUpCities : QueryEntitiesRequestBase<UpCityDto>
    {
        public QueryUpCities(UpCitiesFilter filter)
            : base(filter, $"{ApiResources.Ukrposhta}/cities")
        {
        }
    }

    public sealed class UpCitiesFilter : FilteringItemBase
    {
        public UpCitiesFilter(int? upDistrictId)
        {
            UpDistrictId = upDistrictId;
        }

        [FilteringItemProperty("up_district_id")]
        public int? UpDistrictId { get; }
    }
}