using Telemart.Client.Data.Requests.Base;
using Telemart.Client.Data.WebClient;
using Telemart.Client.TransferObjects.MeestExpress;

namespace Telemart.Client.Data.Requests.Features.MeestExpress
{
    public sealed class QueryMeCities : QueryEntitiesRequestBase<MeCityDto>
    {
        public QueryMeCities(IFilteringItem filter)
            : base(filter, $"{ApiResources.MeestExpress}/cities")
        {
        }
    }

    public sealed class MeCitiesFilter : FilteringItemBase
    {
        public MeCitiesFilter(string meDistrict, string meArea)
        {
            District = meDistrict;
            Area = meArea;
        }

        [FilteringItemProperty("district_ref")]
        public string District { get; }

        [FilteringItemProperty("area_ref")]
        public string Area { get; }
    }
}