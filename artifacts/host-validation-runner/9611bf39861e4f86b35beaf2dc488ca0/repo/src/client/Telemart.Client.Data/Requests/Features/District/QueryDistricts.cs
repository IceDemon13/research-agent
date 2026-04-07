using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects;

namespace Telemart.Client.Data.Requests.Features.District
{
    public sealed class QueryDistricts : QueryEntitiesRequestBase<DistrictDto>
    {
        public QueryDistricts()
            : base($"{ApiResources.Districts}")
        {
        }
    }
}