using Telemart.Client.Data.Requests.Base;
using Telemart.Client.TransferObjects.Ukrposhta;

namespace Telemart.Client.Data.Requests.Features.Ukrposhta
{
    public sealed class QueryUpDistricts : QueryEntitiesRequestBase<UpDistrictDto>
    {
        public QueryUpDistricts()
            : base($"{ApiResources.Ukrposhta}/{ApiResources.Districts}")
        {
        }
    }
}